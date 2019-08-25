using System;
using System.Linq;
using IdentityServer4.Anonymous.Configuration;
using IdentityServer4.Anonymous.Configuration.DependencyInjection;
using IdentityServer4.Anonymous.Endpoints;
using IdentityServer4.Anonymous.Hosting;
using IdentityServer4.Anonymous.Services;
using IdentityServer4.Anonymous.Validation;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Builder extension methods for registering core services.
    /// </summary>
    public static class IdentityServerBuilderExtensionsCore
    {
        #region Methods

        /// <summary>
        /// Adds the anonymous authentication support to IdentityServer4.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddAnonymousAuthentication(this IIdentityServerBuilder builder)
        {
            builder
                .AddAnonymousRequiredPlatformServices()
                .AddAnonymousCoreServices()
                .AddAnonymousCookieAuthentication()
                .AddAnonymousResponseGenerators()
                .AddAnonymousValidators()
                .AddAnonymousDecoratedEndpoints()
                .AddAnonymousPluggableServices();

            return builder;
        }

        /// <summary>
        /// Adds the anonymous authentication support to IdentityServer4.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddAnonymousAuthentication(this IIdentityServerBuilder builder, Action<AnonymousIdentityServerOptions> setupAction)
        {
            builder.Services.Configure(setupAction);
            return builder.AddAnonymousAuthentication();
        }

        /// <summary>
        /// Adds the anonymous authentication support to IdentityServer4.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddAnonymousAuthentication(this IIdentityServerBuilder builder, IConfiguration configuration)
        {
            builder.Services.Configure<AnonymousIdentityServerOptions>(configuration);
            return builder.AddAnonymousAuthentication();
        }
            
        #endregion

        #region Utilities

        /// <summary>
        /// Adds the required platform services.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        private static IIdentityServerBuilder AddAnonymousRequiredPlatformServices(this IIdentityServerBuilder builder)
        {       
            builder.Services.AddOptions();
            builder.Services.AddSingleton(
                resolver => resolver.GetRequiredService<IOptions<AnonymousIdentityServerOptions>>().Value);

            return builder;
        }

        /// <summary>
        /// Adds the default cookie handlers and corresponding configuration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        private static IIdentityServerBuilder AddAnonymousCookieAuthentication(this IIdentityServerBuilder builder)
        {
            builder.Services.AddTransientDecorator<IAuthenticationService, IdentityServerAuthenticationService>();

            return builder;
        }

        /// <summary>
        /// Adds the anonymous core services.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        private static IIdentityServerBuilder AddAnonymousCoreServices(this IIdentityServerBuilder builder)
        {
            builder.Services.AddTransient<IEndpointHandlerProvider, EndpointHandlerProvider>();
            builder.Services.AddScoped<ISharedUserSession, SharedUserSession>();

            builder.Services.AddScopedDecorator<IUserSession, UserSession>();
            builder.Services.AddTransientDecorator<ITokenService, TokenService>();
            builder.Services.AddTransientDecorator<IProfileService, AnonymousProfileService>();

            return builder;
        }

        /// <summary>
        /// Adds the anonymous pluggable services.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        private static IIdentityServerBuilder AddAnonymousPluggableServices(this IIdentityServerBuilder builder)
        {
            builder.Services.TryAddScoped<IAnonymousUserClaimsPrincipalFactory, AnonymousUserClaimsPrincipalFactory>();
            builder.Services.TryAddScoped<IAnonymousUserManager, CookieAnonymousUserManager>();
            builder.Services.TryAddScoped<IAnonymousSignInManager, AnonymousSignInManager>();
            builder.Services.TryAddScoped<IAnonymousUserFactory, AnonymousUserFactory>();

            return builder;
        }

        /// <summary>
        /// Adds the anonymous response generators.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        private static IIdentityServerBuilder AddAnonymousResponseGenerators(this IIdentityServerBuilder builder)
        {
            builder.Services.AddTransientDecorator<IAuthorizeInteractionResponseGenerator, AnonymousAuthorizeInteractionResponseGenerator>();

            return builder;
        }

        /// <summary>
        /// Adds the anonymous validators.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        private static IIdentityServerBuilder AddAnonymousValidators(this IIdentityServerBuilder builder)
        {
            builder.Services.AddTransientDecorator<IAuthorizeRequestValidator, AnonymousAuthorizeRequestValidator>();

            return builder;
        }

        /// <summary>
        /// Adds the anonymous decorated endpoints.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        private static IIdentityServerBuilder AddAnonymousDecoratedEndpoints(this IIdentityServerBuilder builder)
        {
            builder.AddEndpointHandlerDecorator<AnonymousAuthorizeEndpoint>("/connect/authorize");

            return builder;
        }

        private static IIdentityServerBuilder AddEndpointHandlerDecorator<T>(this IIdentityServerBuilder builder, string endpointPath)
            where T : class, IEndpointHandler
        {
            if (builder.Services.Any(sd => 
                sd.ImplementationInstance is DecoratedEndpoint dEndpoint 
                  && dEndpoint.Path == endpointPath))
            {
                throw new InvalidOperationException("Endpoint handler decorator already registered for endpoint path: " + endpointPath + ".");
            }

            var registration = builder.Services.SingleOrDefault(sd =>
            {
                return sd.ServiceType == typeof(Endpoint)
                         && sd.Lifetime == ServiceLifetime.Singleton
                           && sd.ImplementationInstance is Endpoint endpoint
                             && endpoint.Path == endpointPath;
            });

            var sourceEndpoint = registration?.ImplementationInstance as Endpoint;
            if (sourceEndpoint != null)
            {
                builder.Services.AddSingleton(
                    new DecoratedEndpoint(
                        sourceEndpoint.Name, 
                        sourceEndpoint.Path, 
                        sourceEndpoint.Handler, 
                        typeof(T)
                    )
                );
                builder.Services.AddTransient<T>();

                sourceEndpoint.Handler = typeof(T);
            }

            return builder;
        }

        internal static void AddTransientDecorator<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddDecorator<TService>();
            services.AddTransient<TService, TImplementation>();
        }

        internal static void AddScopedDecorator<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddDecorator<TService>();
            services.AddScoped<TService, TImplementation>();
        }

        internal static void AddDecorator<TService>(this IServiceCollection services)
        {
            var registration = services.LastOrDefault(x => x.ServiceType == typeof(TService));
            if (registration == null)
            {
                throw new InvalidOperationException("Service type: " + typeof(TService).Name + " not registered.");
            }
            if (services.Any(x => x.ServiceType == typeof(Decorator<TService>)))
            {
                throw new InvalidOperationException("Decorator already registered for type: " + typeof(TService).Name + ".");
            }

            services.Remove(registration);

            if (registration.ImplementationInstance != null)
            {
                var type = registration.ImplementationInstance.GetType();
                var innerType = typeof(Decorator<,>).MakeGenericType(typeof(TService), type);
                services.Add(new ServiceDescriptor(typeof(Decorator<TService>), innerType, ServiceLifetime.Transient));
                services.Add(new ServiceDescriptor(type, registration.ImplementationInstance));
            }
            else if (registration.ImplementationFactory != null)
            {
                services.Add(new ServiceDescriptor(typeof(Decorator<TService>), provider =>
                {
                    return new DisposableDecorator<TService>((TService)registration.ImplementationFactory(provider));
                }, registration.Lifetime));
            }
            else
            {
                var type = registration.ImplementationType;
                var innerType = typeof(Decorator<,>).MakeGenericType(typeof(TService), registration.ImplementationType);
                services.Add(new ServiceDescriptor(typeof(Decorator<TService>), innerType, ServiceLifetime.Transient));
                services.Add(new ServiceDescriptor(type, type, registration.Lifetime));
            }
        }
            
        #endregion
    }
}