using System;
using System.Linq;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Configuration.DependencyInjection;
using AnonymousIdentity.Hosting;
using AnonymousIdentity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// DI extension methods for adding AnonymousIdentity
    /// </summary>
    public static class AnonymousIdentityServiceCollectionExtensions
    {
        #region Methods

        /// <summary>
        /// Adds the AnonymousIdentity.
        /// </summary>
        /// <param name="sevices">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddAnonymousIdentity(this IServiceCollection sevices)
        {
            return sevices
                .AddRequiredPlatformServices()
                .AddCookieAuthentication()
                .AddCoreServices()
                .AddPluggableServices();
        }

        /// <summary>
        /// Adds the AnonymousIdentity.
        /// </summary>
        /// <param name="sevices">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddAnonymousIdentity(this IServiceCollection sevices, Action<AnonymousIdentityServerOptions> setupAction)
        {
            sevices.Configure(setupAction);
            return sevices.AddAnonymousIdentity();
        }

        /// <summary>
        /// Adds the AnonymousIdentity.
        /// </summary>
        /// <param name="sevices">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddAnonymousIdentity(this IServiceCollection sevices, IConfiguration configuration)
        {
            sevices.Configure<AnonymousIdentityServerOptions>(configuration);
            return sevices.AddAnonymousIdentity();
        }
            
        #endregion

        #region Utilities

        /// <summary>
        /// Adds the required platform services.
        /// </summary>
        /// <param name="sevices">The sevices.</param>
        /// <returns></returns>
        private static IServiceCollection AddRequiredPlatformServices(this IServiceCollection sevices)
        {       
            sevices.AddOptions();
            sevices.AddSingleton(
                resolver => resolver.GetRequiredService<IOptions<AnonymousIdentityServerOptions>>().Value);

            return sevices;
        }

        /// <summary>
        /// Adds the default cookie handlers and corresponding configuration.
        /// </summary>
        /// <param name="sevices">The sevices.</param>
        /// <returns></returns>
        private static IServiceCollection AddCookieAuthentication(this IServiceCollection sevices)
        {
            sevices.AddTransientDecorator<IAuthenticationService, AnonymousIdentityAuthenticationService>();

            return sevices;
        }

        /// <summary>
        /// Adds the core services.
        /// </summary>
        /// <param name="sevices">The sevices.</param>
        /// <returns></returns>
        private static IServiceCollection AddCoreServices(this IServiceCollection sevices)
        {
            sevices.AddScoped<ISharedUserSession, SharedUserSession>();

            return sevices;
        }

        /// <summary>
        /// Adds the pluggable services.
        /// </summary>
        /// <param name="sevices">The sevices.</param>
        /// <returns></returns>
        private static IServiceCollection AddPluggableServices(this IServiceCollection sevices)
        {
            sevices.TryAddScoped<IAnonymousUserClaimsPrincipalFactory, AnonymousUserClaimsPrincipalFactory>();
            sevices.TryAddScoped<IAnonymousUserManager, CookieAnonymousUserManager>();
            sevices.TryAddScoped<IAnonymousSignInManager, AnonymousSignInManager>();
            sevices.TryAddScoped<IAnonymousUserFactory, AnonymousUserFactory>();

            return sevices;
        }

        private static void AddTransientDecorator<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddDecorator<TService>();
            services.AddTransient<TService, TImplementation>();
        }

        private static void AddScopedDecorator<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddDecorator<TService>();
            services.AddScoped<TService, TImplementation>();
        }

        private static void AddDecorator<TService>(this IServiceCollection services)
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