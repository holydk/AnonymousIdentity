using System;
using System.Collections.Generic;
using System.Linq;
using IdentityServer4.Anonymous.Configuration.DependencyInjection;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;

namespace IdentityServer4.Anonymous.Services
{
    /// <summary>
    /// Represents a endpoint handlers factory.
    /// </summary>
    internal class EndpointHandlerProvider : IEndpointHandlerProvider
    {
        #region Fields

        private readonly IEnumerable<Endpoint> _endpoints;
        private readonly IEnumerable<DecoratedEndpoint> _decoratedEndpoints;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private  HttpContext _httpContext => _httpContextAccessor.HttpContext;
            
        #endregion

        #region Ctor

        public EndpointHandlerProvider(
            IEnumerable<Endpoint> endpoints,
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