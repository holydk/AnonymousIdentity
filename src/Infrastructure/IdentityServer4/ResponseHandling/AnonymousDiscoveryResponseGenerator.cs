using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Infrastructure.IdentityServer4.Configuration.DependencyInjection;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.ResponseHandling
{
    /// <summary>
///     /// this decorates the real discovery response generator to include additional claims.
    /// </summary>
    internal class AnonymousDiscoveryResponseGenerator : IDiscoveryResponseGenerator
    {
        #region Fields

        private readonly IDiscoveryResponseGenerator _inner;
        private readonly IdentityServerOptions _options;
        private readonly AnonymousIdentityServerOptions _anonIdsrvOptions;

        #endregion

        #region Ctor

        public AnonymousDiscoveryResponseGenerator(
            Decorator<IDiscoveryResponseGenerator> decorator,
            IdentityServerOptions options,
            AnonymousIdentityServerOptions anonIdsrvOptions)
        {
            _inner = decorator.Instance;
            _options = options;
            _anonIdsrvOptions = anonIdsrvOptions;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the discovery document.
        /// </summary>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="issuerUri">The issuer URI.</param>
        public async Task<Dictionary<string, object>> CreateDiscoveryDocumentAsync(string baseUrl, string issuerUri)
        {
            var entries = await _inner.CreateDiscoveryDocumentAsync(baseUrl, issuerUri);

            // response modes
            if (_options.Discovery.ShowResponseModes)
            {
                if (entries.ContainsKey(IdentityModel.OidcConstants.Discovery.ResponseModesSupported))
                {
                    var response_modes = (string[])entries[IdentityModel.OidcConstants.Discovery.ResponseModesSupported];                   
                    if (!response_modes.Any(rm => rm == OidcConstants.ResponseModes.Json))
                    {
                        entries[IdentityModel.OidcConstants.Discovery.ResponseModesSupported] = new List<string>(response_modes)
                        {
                            OidcConstants.ResponseModes.Json
                        }
                        .ToArray();
                    }
                }
            }

            // claims
            if (_anonIdsrvOptions.AlwaysIncludeAnonymousIdInProfile && _options.Discovery.ShowClaims)
            {
                if (entries.ContainsKey(IdentityModel.OidcConstants.Discovery.ClaimsSupported))
                {
                    var claims = (string[])entries[IdentityModel.OidcConstants.Discovery.ClaimsSupported];
                    if (!claims.Any(rm => rm == JwtClaimTypes.AnonymousId))
                    {
                        entries[IdentityModel.OidcConstants.Discovery.ClaimsSupported] = new List<string>(claims)
                        {
                            JwtClaimTypes.AnonymousId
                        }
                        .ToArray();
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Creates the JWK document.
        /// </summary>
        public Task<IEnumerable<JsonWebKey>> CreateJwkDocumentAsync()
        {
            return _inner.CreateJwkDocumentAsync();
        }

        #endregion
    }
}