using System;
using System.Collections.Generic;
using System.Linq;
using AnonymousIdentity.Infrastructure.IdentityServer4.Configuration.DependencyInjection;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Services
{
    /// <summary>
    /// Represents a endpoint handlers factory.
    /// </summary>
    internal class EndpointHandlerProvider : IEndpointHandlerProvider
    {
        #region Fields

        private readonly IEnumerable<global::IdentityServer4.Hosting.Endpoint> _endpoints;
        private readonly IEnumerable<DecoratedEndpoint> _decoratedEndpoints;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private HttpContext _httpContext => _httpContextAccessor.HttpContext;
            
        #endregion

        #region Ctor

        public EndpointHandlerProvider(
            IEnumerable<global::IdentityServer4.Hosting.Endpoint> endpoints,
            IEnumerable<DecoratedEndpoint> decoratedEndpoints,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _endpoints = endpoints;
            _decoratedEndpoints = decoratedEndpoints;
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets original endpoint handler by path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The endpoint handler.</returns>
        public IEndpointHandler GetByPath(string path)
        {
            if (_decoratedEndpoints.Any())
            {
                var decoratedEndpoint = _decoratedEndpoints.FirstOrDefault(d => d.Path == path);
                if (decoratedEndpoint != null)
                {
                    return GetEndpointHandler(decoratedEndpoint.SourceHandlerType);
                }
            }

            var endpoint = _endpoints.FirstOrDefault(d => d.Path == path);
            if (endpoint != null)
            {
                return GetEndpointHandler(endpoint.Handler);
            }

            return null;
        }
            
        #endregion

        #region Utilities

        private IEndpointHandler GetEndpointHandler(Type handlerType)
        {
            return _httpContext.RequestServices.GetService(handlerType) as IEndpointHandler;
        }
            
        #endregion
    }
}