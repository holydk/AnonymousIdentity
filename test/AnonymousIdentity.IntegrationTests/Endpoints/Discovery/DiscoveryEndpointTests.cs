using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel.Client;
using IdentityServer4.Models;
using NUnit.Framework;

namespace AnonymousIdentity.IntegrationTests.Endpoints.Discovery
{
    public class DiscoveryEndpointTests
    {
        #region Fields

        private IdentityServerPipeline _mockPipeline;
            
        #endregion

        #region Init

        [SetUp]
        public void Init()
        {
            _mockPipeline = new IdentityServerPipeline();
            _mockPipeline.Initialize();
        }
            
        #endregion

        [Test]
        public async Task Discovery_response_should_contains_json_response_mode()
        {
            var result = await _mockPipeline.BackChannelClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = IdentityServerPipeline.BaseUrl
            });

            result.ResponseModesSupported.Should().Contain(OidcConstants.ResponseModes.Json);
        }

        [Test]
        public async Task Discovery_response_should_contains_aid_always()
        {
            var result = await _mockPipeline.BackChannelClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = IdentityServerPipeline.BaseUrl
            });

            result.ClaimsSupported.Should().Contain(JwtClaimTypes.AnonymousId);
        }

        [Test]
        public async Task Discovery_response_dont_should_contains_aid_if_not_requested()
        {
            _mockPipeline.AnonymousOptions.AlwaysIncludeAnonymousIdInProfile = false;
            var result = await _mockPipeline.BackChannelClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = IdentityServerPipeline.BaseUrl
            });

            result.ClaimsSupported.Should().NotContain(JwtClaimTypes.AnonymousId);
        }

        [Test]
        public async Task Discovery_response_should_contains_aid_if_requested()
        {
            _mockPipeline.AnonymousOptions.AlwaysIncludeAnonymousIdInProfile = false;
            var profile = new IdentityResources.Profile();
            profile.UserClaims.Add(JwtClaimTypes.AnonymousId);   
            _mockPipeline.IdentityScopes.Add(profile);

            var result = await _mockPipeline.BackChannelClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = IdentityServerPipeline.BaseUrl
            });

            result.ClaimsSupported.Should().Contain(JwtClaimTypes.AnonymousId);
        }
    }
}