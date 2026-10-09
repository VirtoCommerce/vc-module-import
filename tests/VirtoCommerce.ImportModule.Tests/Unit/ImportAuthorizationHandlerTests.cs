using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Data.Authorization;
using VirtoCommerce.ImportModule.Web.Authorization;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportAuthorizationHandlerTests
    {
        private const string MemberId = "member-1";
        private const string OrganizationId = "organization-1";

        [Fact]
        public async Task Anonymous_Caller_Is_Refused()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            var resource = new AuthorizationInfo();

            var context = await Authorize(user, resource);

            Assert.False(context.HasSucceeded);
            Assert.Null(resource.OrganizationId);
        }

        [Fact]
        public async Task Unauthenticated_Caller_Carrying_An_Employee_Member_Id_Is_Refused()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("memberId", MemberId)]));
            var resource = new AuthorizationInfo();
            var memberResolver = CreateMemberResolver(
                new Employee { Id = MemberId, Organizations = [OrganizationId] },
                new Organization { Id = OrganizationId });

            var context = await Authorize(user, resource, memberResolver);

            Assert.False(context.HasSucceeded);
            Assert.Null(resource.OrganizationId);
        }

        [Fact]
        public async Task Caller_Without_Permission_Or_Organization_Is_Refused()
        {
            var user = CreateUser(new Claim("memberId", MemberId));
            var resource = new AuthorizationInfo();
            var memberResolver = CreateMemberResolver(new Contact { Id = MemberId });

            var context = await Authorize(user, resource, memberResolver);

            Assert.False(context.HasSucceeded);
            Assert.Null(resource.OrganizationId);
        }

        [Fact]
        public async Task Permission_Holder_Without_Organization_Is_Admitted_Unscoped()
        {
            var user = CreateUser(new Claim("permission", ModuleConstants.Security.Permissions.Access));
            var resource = new AuthorizationInfo();

            var context = await Authorize(user, resource);

            Assert.True(context.HasSucceeded);
            Assert.Null(resource.OrganizationId);
        }

        [Fact]
        public async Task Organization_Employee_Without_Permission_Is_Refused_Even_For_An_Organization_Scoped_Resource()
        {
            var user = CreateUser(new Claim("memberId", MemberId));
            var resource = new AuthorizationInfo();
            var memberResolver = CreateMemberResolver(
                new Employee { Id = MemberId, Organizations = [OrganizationId] },
                new Organization { Id = OrganizationId });

            var context = await Authorize(user, resource, memberResolver);

            Assert.False(context.HasSucceeded);
            Assert.Null(resource.OrganizationId);
        }

        [Fact]
        public async Task Permission_Holder_Who_Is_An_Organization_Employee_Is_Scoped_To_Its_Organization()
        {
            var user = CreateUser(
                new Claim("permission", ModuleConstants.Security.Permissions.Access),
                new Claim("memberId", MemberId));
            var resource = new AuthorizationInfo();
            var memberResolver = CreateMemberResolver(
                new Employee { Id = MemberId, Organizations = [OrganizationId] },
                new Organization { Id = OrganizationId });

            var context = await Authorize(user, resource, memberResolver);

            Assert.True(context.HasSucceeded);
            Assert.Equal(OrganizationId, resource.OrganizationId);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Employee_Without_An_Organization_Scoped_Resource_Is_Not_Admitted(bool hasProfileResource)
        {
            var user = CreateUser(new Claim("memberId", MemberId));
            object resource = hasProfileResource ? new ImportProfile() : null;
            var memberResolver = CreateMemberResolver(
                new Employee { Id = MemberId, Organizations = [OrganizationId] },
                new Organization { Id = OrganizationId });

            var context = await Authorize(user, resource, memberResolver);

            Assert.False(context.HasSucceeded);
        }

        private static async Task<AuthorizationHandlerContext> Authorize(ClaimsPrincipal user, object resource, IMemberResolver memberResolver = null)
        {
            var handler = new ImportAuthorizationHandler(memberResolver ?? Mock.Of<IMemberResolver>());
            var requirement = new ImportAuthorizationRequirement(ModuleConstants.Security.Permissions.Access);
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);

            await handler.HandleAsync(context);

            return context;
        }

        private static ClaimsPrincipal CreateUser(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        private static IMemberResolver CreateMemberResolver(params Member[] members)
        {
            var byId = new Dictionary<string, Member>();
            foreach (var member in members)
            {
                byId[member.Id] = member;
            }

            var resolver = new Mock<IMemberResolver>();
            resolver
                .Setup(x => x.ResolveMemberByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => byId.GetValueOrDefault(id));

            return resolver.Object;
        }
    }
}
