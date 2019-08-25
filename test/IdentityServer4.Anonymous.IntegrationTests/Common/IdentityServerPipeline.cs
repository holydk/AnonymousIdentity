using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel.Client;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer4.Anonymous.IntegrationTests
{
    public class IdentityServerPipeline
    {
        public static class DefaultRoutePaths
        {
            public const string Login = "/account/login";
            public const string Logout = "/account/logout";
            public const string Consent = "/consent";
            public const string Error = "/home/error";
            public const string DeviceVerification = "/device";
        }

        public const string BaseUrl = "https://server";
        public const string LoginPage = BaseUrl + "/account/login";
        public const string ConsentPage = BaseUrl + "/account/consent";
        public const string ErrorPage = BaseUrl + "/home/error";

        public const string AuthorizeEndpoint = BaseUrl + "/connect/authorize";

        public TestServer Server { get; set; }
        public HttpMessageHandler Handler { get; set; }

        public BrowserClient BrowserClient { get; set; }
        public HttpClient BackChannelClient { get; set; }

        public IdentityServerOptions Options { get; set; }
        public List<Client> Clients { get; set; } = new List<Client>();
        public List<IdentityResource> IdentityScopes { get; set; } = new List<IdentityResource>();
        public List<ApiResource> ApiScopes { get; set; } = new List<ApiResource>();
        public List<TestUser> Users { get; set; } = new List<TestUser>();

        public bool LoginWasCalled { get; set; }
        public AuthorizationRequest LoginRequest { get; set; }
        public ClaimsPrincipal Subject { get; set; }

        public bool ErrorWasCalled { get; set; }
        public ErrorMessage ErrorMessage { get; set; }

        public bool ConsentWasCalled { get; set; }
        public AuthorizationRequest ConsentRequest { get; set; }
        public ConsentResponse ConsentResponse { get; set; }

        public event Action<IServiceCollection> OnPostConfigureServices = services => { };

        public void Initialize(string basePath = null, bool enableLogging = false)
        {
            var builder = new WebHostBuilder();
            builder.ConfigureServices(ConfigureServices);
            builder.Configure(app=>
            {
                if (basePath != null)
                {
                    app.Map(basePath, map =>
                    {
                        ConfigureApp(map);
                    });
                }
                else
                {
                    ConfigureApp(app);
                }
            });

            if (enableLogging)
            {
                //builder.ConfigureLogging((ctx, b) => b.AddConsole());
            }

            Server = new TestServer(builder);
            Handler = Server.CreateHandler();
            
            BrowserClient = new BrowserClient(new BrowserHandler(Handler));
            BackChannelClient = new HttpClient(Handler);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication();
            
            services.AddIdentityServer(options =>
            {
                Options = options;

                options.Events = new EventsOptions
                {
                    RaiseErrorEvents = true,
                    RaiseFailureEvents = true,
                    RaiseInformationEvents = true,
                    RaiseSuccessEvents = true
                };
            })
            .AddInMemoryClients(Clients)
            .AddInMemoryIdentityResources(IdentityScopes)
            .AddInMemoryApiResources(ApiScopes)
            .AddTestUsers(Users)
            .AddDeveloperSigningCredential(persistKey: false)
            .AddAnonymousAuthentication();

            OnPostConfigureServices?.Invoke(services);
        }

        public void ConfigureApp(IApplicationBuilder app)
        {
            app.UseIdentityServer();

            app.Map(DefaultRoutePaths.Login, path =>
            {
                path.Run(ctx => OnLogin(ctx));
            });
            app.Map(DefaultRoutePaths.Logout, path =>
            {
                path.Run(ctx => OnLogout(ctx));
            });
            app.Map(DefaultRoutePaths.Consent, path =>
            {
                path.Run(ctx => OnConsent(ctx));
            });
            app.Map(DefaultRoutePaths.Error, path =>
            {
                path.Run(ctx => OnError(ctx));
            });
        }

        private async Task OnLogin(HttpContext ctx)
        {
            LoginWasCalled = true;
            await ReadLoginRequest(ctx);
            await IssueLoginCookie(ctx);
        }

        public async Task LoginAsync(ClaimsPrincipal subject)
        {
            var old = BrowserClient.AllowAutoRedirect;
            BrowserClient.AllowAutoRedirect = false;

            Subject = subject;
            await BrowserClient.GetAsync(LoginPage);

            BrowserClient.AllowAutoRedirect = old;
        }

        public async Task LoginAsync(string subject)
        {
            await LoginAsync(new IdentityServerUser(subject).CreatePrincipal());
        }

        private async Task ReadLoginRequest(HttpContext ctx)
        {
            var interaction = ctx.RequestServices.GetRequiredService<IIdentityServerInteractionService>();
            LoginRequest = await interaction.GetAuthorizationContextAsync(ctx.Request.Query["returnUrl"].FirstOrDefault());
        }

        private async Task IssueLoginCookie(HttpContext ctx)
        {
            if (Subject != null)
            {
                var props = new AuthenticationProperties();
                await ctx.SignInAsync(Subject, props);
                Subject = null;
                var url = ctx.Request.Query[Options.UserInteraction.LoginReturnUrlParameter].FirstOrDefault();
                if (url != null)
                {
                    ctx.Response.Redirect(url);
                }
            }
        }

        public Cookie GetSessionCookie()
        {
            return BrowserClient.GetCookie(BaseUrl, IdentityServer4.IdentityServerConstants.DefaultCheckSessionCookieName);
        }

        public Cookie GetSharedSessionCookie()
        {
            return BrowserClient.GetCookie(BaseUrl, IdentityServerConstants.DefaultCheckSharedSessionCookieName);
        }

        public Cookie GetAnonymousIdCookie()
        {
            return BrowserClient.GetCookie(BaseUrl, IdentityServerConstants.DefaultCheckAnonymousIdCookieName);
        }

        private Task OnLogout(HttpContext ctx)
        {
            return Task.CompletedTask;
        }

        private async Task OnConsent(HttpContext ctx)
        {
            ConsentWasCalled = true;
            await ReadConsentMessage(ctx);
            await CreateConsentResponse(ctx);
        }

        private async Task ReadConsentMessage(HttpContext ctx)
        {
            var interaction = ctx.RequestServices.GetRequiredService<IIdentityServerInteractionService>();
            ConsentRequest = await interaction.GetAuthorizationContextAsync(ctx.Request.Query["returnUrl"].FirstOrDefault());
        }

        private async Task CreateConsentResponse(HttpContext ctx)
        {
            if (ConsentRequest != null && ConsentResponse != null)
            {
                var interaction = ctx.RequestServices.GetRequiredService<IIdentityServerInteractionService>();
                await interaction.GrantConsentAsync(ConsentRequest, ConsentResponse);
                ConsentResponse = null;

                var url = ctx.Request.Query[Options.UserInteraction.ConsentReturnUrlParameter].FirstOrDefault();
                if (url != null)
                {
                    ctx.Response.Redirect(url);
                }
            }
        }

        private async Task OnError(HttpContext ctx)
        {
            ErrorWasCalled = true;
            await ReadErrorMessage(ctx);
        }

        private async Task ReadErrorMessage(HttpContext ctx)
        {
            var interaction = ctx.RequestServices.GetRequiredService<IIdentityServerInteractionService>();
            ErrorMessage = await interaction.GetErrorContextAsync(ctx.Request.Query["errorId"].FirstOrDefault());
        }

        public string CreateAuthorizeUrl(
            string clientId = null,
            string responseType = null,
            string scope = null,
            string redirectUri = null,
            string state = null,
            string nonce = null,
            string loginHint = null,
            string acrValues = null,
            string responseMode = null,
            string codeChallenge = null,
            string codeChallengeMethod = null,
            object extra = null)
        {
            var url = new RequestUrl(AuthorizeEndpoint).CreateAuthorizeUrl(
                clientId: clientId,
                responseType: responseType,
                scope: scope,
                redirectUri: redirectUri,
                state: state,
                nonce: nonce,
                loginHint: loginHint,
                acrValues: acrValues,
                responseMode: responseMode,
                codeChallenge: codeChallenge,
                codeChallengeMethod: codeChallengeMethod,
                extra: extra);
            return url;
        }
    }
}