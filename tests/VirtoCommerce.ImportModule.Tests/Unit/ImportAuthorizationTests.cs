using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Web.Authorization;
using VirtoCommerce.ImportModule.Web.Controllers.Api;
using VirtoCommerce.Platform.Core;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    /// <summary>
    /// Authorization tests for <see cref="ImportAuthorizationHandler"/> and the import API controllers.
    /// </summary>
    public class ImportAuthorizationTests
    {
        private const string OrganizationId = "org-1";
        private const string OtherOrganizationId = "org-2";
        private const string EmployeeId = "employee-1";

        private readonly Mock<IMemberResolver> _memberResolver = new();
        private readonly Mock<IImportRunHistorySearchService> _historySearchService = new();
        private readonly Mock<IImportProfilesSearchService> _profilesSearchService = new();

        public ImportAuthorizationTests()
        {
            _memberResolver
                .Setup(x => x.ResolveMemberByIdAsync(EmployeeId))
                .ReturnsAsync(new Employee { Id = EmployeeId, Organizations = new List<string> { OrganizationId } });
            _memberResolver
                .Setup(x => x.ResolveMemberByIdAsync(OrganizationId))
                .ReturnsAsync(new Organization { Id = OrganizationId, Name = "Own organization" });

            _historySearchService
                .Setup(x => x.SearchAsync(It.IsAny<SearchImportRunHistoryCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new SearchImportRunHistoryResult());
            _profilesSearchService
                .Setup(x => x.SearchAsync(It.IsAny<SearchImportProfilesCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new SearchImportProfilesResult());
        }

        // ImportController

        [Fact]
        public async Task SearchImportRunHistory_Returns_Unauthorized_For_Anonymous_User()
        {
            var controller = CreateImportController(Anonymous());

            var result = await controller.SearchImportRunHistory(new SearchImportRunHistoryCriteria());

            Assert.IsType<UnauthorizedResult>(result.Result);
            _historySearchService.Verify(x => x.SearchAsync(It.IsAny<SearchImportRunHistoryCriteria>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task SearchImportRunHistory_Scopes_Organization_Member_To_Own_Organization()
        {
            var controller = CreateImportController(OrganizationMember());

            await controller.SearchImportRunHistory(new SearchImportRunHistoryCriteria { UserId = OtherOrganizationId });

            _historySearchService.Verify(x => x.SearchAsync(It.Is<SearchImportRunHistoryCriteria>(c => c.UserId == OrganizationId), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task SearchImportProfiles_Returns_Unauthorized_For_Anonymous_User()
        {
            var controller = CreateImportController(Anonymous());

            var result = await controller.SearchImportProfiles(new SearchImportProfilesCriteria());

            Assert.IsType<UnauthorizedResult>(result.Result);
            _profilesSearchService.Verify(x => x.SearchAsync(It.IsAny<SearchImportProfilesCriteria>(), It.IsAny<bool>()), Times.Never);
        }

        // OrganizationController

        [Fact]
        public async Task GetOrganizationInfo_Returns_Unauthorized_For_Anonymous_User()
        {
            var controller = CreateOrganizationController(Anonymous());

            var result = await controller.GetOrganizationInfo(OtherOrganizationId);

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        // Helpers

        private IAuthorizationService CreateAuthorizationService()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorizationCore();
            services.AddSingleton(_memberResolver.Object);
            services.AddTransient<IAuthorizationHandler, ImportAuthorizationHandler>();

            return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
        }

        private ImportController CreateImportController(ClaimsPrincipal user)
        {
            return new ImportController(
                Mock.Of<IDataImporterRegistrar>(),
                Mock.Of<IImportRunService>(),
                _profilesSearchService.Object,
                _historySearchService.Object,
                Mock.Of<IImportProfileCrudService>(),
                CreateAuthorizationService(),
                Mock.Of<IDataImporterFactory>())
            {
                ControllerContext = CreateControllerContext(user),
            };
        }

        private OrganizationController CreateOrganizationController(ClaimsPrincipal user)
        {
            return new OrganizationController(CreateAuthorizationService(), _memberResolver.Object)
            {
                ControllerContext = CreateControllerContext(user),
            };
        }

        private static ControllerContext CreateControllerContext(ClaimsPrincipal user)
        {
            return new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
        }

        private static ClaimsPrincipal Anonymous()
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        private static ClaimsPrincipal Authenticated(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
        }

        private static ClaimsPrincipal OrganizationMember()
        {
            return Authenticated(new Claim(PlatformConstants.Security.Claims.MemberIdClaimType, EmployeeId));
        }
    }
}
