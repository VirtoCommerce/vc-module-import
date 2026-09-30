using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Web.Authorization;
using VirtoCommerce.ImportModule.Web.Controllers.Api;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class OrganizationControllerTests
    {
        private const string OwnOrganizationId = "organization-1";
        private const string OtherOrganizationId = "organization-2";
        private const string ContactId = "contact-1";

        [Fact]
        public async Task Scoped_Caller_Without_An_Id_Gets_Its_Own_Organization()
        {
            var fixture = new Fixture(OwnOrganizationId);

            var result = await fixture.Controller.GetOrganizationInfo(null);

            var info = Assert.IsType<OrganizationInfo>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal(OwnOrganizationId, info.OrganizationId);
            Assert.Equal("Own", info.OrganizationName);
        }

        [Fact]
        public async Task Scoped_Caller_Asking_For_Another_Organization_Is_Refused()
        {
            var fixture = new Fixture(OwnOrganizationId);

            var result = await fixture.Controller.GetOrganizationInfo(OtherOrganizationId);

            Assert.IsType<UnauthorizedResult>(result.Result);
            fixture.MemberResolver.Verify(x => x.ResolveMemberByIdAsync(OtherOrganizationId), Times.Never);
        }

        [Fact]
        public async Task Unscoped_Caller_Gets_Any_Organization_By_Id()
        {
            var fixture = new Fixture(null);

            var result = await fixture.Controller.GetOrganizationInfo(OtherOrganizationId);

            var info = Assert.IsType<OrganizationInfo>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal(OtherOrganizationId, info.OrganizationId);
            Assert.Equal("Other", info.OrganizationName);
        }

        [Fact]
        public async Task Member_That_Is_Not_An_Organization_Yields_No_Info()
        {
            var fixture = new Fixture(null);

            var result = await fixture.Controller.GetOrganizationInfo(ContactId);

            Assert.IsType<OkResult>(result.Result);
        }

        private sealed class Fixture
        {
            public Fixture(string scopedOrganizationId)
            {
                var authorizationService = new Mock<IAuthorizationService>();
                authorizationService
                    .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                    .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((_, resource, _) =>
                    {
                        if (scopedOrganizationId != null)
                        {
                            ((AuthorizationInfo)resource).OrganizationId = scopedOrganizationId;
                        }
                    })
                    .ReturnsAsync(AuthorizationResult.Success());

                var members = new Dictionary<string, Member>
                {
                    [OwnOrganizationId] = new Organization { Id = OwnOrganizationId, Name = "Own" },
                    [OtherOrganizationId] = new Organization { Id = OtherOrganizationId, Name = "Other" },
                    [ContactId] = new Contact { Id = ContactId, Name = "Some Contact" },
                };

                MemberResolver = new Mock<IMemberResolver>();
                MemberResolver
                    .Setup(x => x.ResolveMemberByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((string id) => members.GetValueOrDefault(id));

                Controller = new OrganizationController(authorizationService.Object, MemberResolver.Object)
                {
                    ControllerContext = new ControllerContext
                    {
                        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) },
                    },
                };
            }

            public OrganizationController Controller { get; }

            public Mock<IMemberResolver> MemberResolver { get; }
        }
    }
}
