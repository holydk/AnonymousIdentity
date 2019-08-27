using System;
using IdentityServer4.Hosting;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Configuration.DependencyInjection
{
    internal class DecoratedEndpoint : Endpoint
    {
        public DecoratedEndpoint(string name, string path, Type sourceType, Type decoratedType)
            : base(name, path, decoratedType)
        {
            SourceHandlerType = sourceType;
        }

        /// <summary>
        /// Gets or sets the source endpoint handler type.
        /// </summary>
        /// <value></value>
        public Type SourceHandlerType { get; set; }
    }
}