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
using VirtoCommerce.ImportModule.Core;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Authorization;
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
        private readonly Mock<IImportRunService> _importRunService = new();
        private readonly Mock<IDataImporterFactory> _dataImporterFactory = new();

        public ImportAuthorizationTests()
        {
            _memberResolver
                .Setup(x => x.ResolveMemberByIdAsync(EmployeeId))
                .ReturnsAsync(new Employee { Id = EmployeeId, Organizations = new List<string> { OrganizationId } });
            _memberResolver
                .Setup(x => x.ResolveMemberByIdAsync(OrganizationId))
                .ReturnsAsync(new Organization { Id = OrganizationId, Name = "Own organization" });
            _memberResolver
                .Setup(x => x.ResolveMemberByIdAsync(OtherOrganizationId))
                .ReturnsAsync(new Organization { Id = OtherOrganizationId, Name = "Other organization" });

            _historySearchService
                .Setup(x => x.SearchAsync(It.IsAny<SearchImportRunHistoryCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new SearchImportRunHistoryResult());
            _profilesSearchService
                .Setup(x => x.SearchAsync(It.IsAny<SearchImportProfilesCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new SearchImportProfilesResult());
        }

        // Handler

        [Fact]
        public async Task Handler_Fails_For_Anonymous_User()
        {
            var user = Anonymous(new Claim(PlatformConstants.Security.Claims.MemberIdClaimType, EmployeeId));
            var authorizationInfo = new AuthorizationInfo();

            var result = await AuthorizeAsync(user, authorizationInfo);

            Assert.False(result.Succeeded);
            Assert.Null(authorizationInfo.OrganizationId);
        }

        [Fact]
        public async Task Handler_Fails_For_Authenticated_User_Without_Permission_Or_Organization()
        {
            var result = await AuthorizeAsync(Authenticated(), new AuthorizationInfo());

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task Handler_Succeeds_For_User_With_Permission_Without_Organization_Scope()
        {
            var authorizationInfo = new AuthorizationInfo();

            var result = await AuthorizeAsync(WithAccessPermission(), authorizationInfo);

            Assert.True(result.Succeeded);
            Assert.Null(authorizationInfo.OrganizationId);
        }

        [Fact]
        public async Task Handler_Succeeds_For_Organization_Member_And_Scopes_To_Organization()
        {
            var authorizationInfo = new AuthorizationInfo();

            var result = await AuthorizeAsync(OrganizationMember(), authorizationInfo);

            Assert.True(result.Succeeded);
            Assert.Equal(OrganizationId, authorizationInfo.OrganizationId);
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

        [Fact]
        public async Task RunImport_Returns_Unauthorized_For_Anonymous_User_When_Importer_Has_No_Requirement()
        {
            var importer = new Mock<IDataImporter>();
            importer.SetupGet(x => x.AuthorizationRequirement).Returns((IAuthorizationRequirement)null);
            _dataImporterFactory.Setup(x => x.Create(It.IsAny<string>())).Returns(importer.Object);
            var controller = CreateImportController(Anonymous());

            var result = await controller.RunImport(new ImportProfile { DataImporterType = "AnyImporter" });

            Assert.IsType<UnauthorizedResult>(result.Result);
            _importRunService.Verify(x => x.RunImportBackgroundJob(It.IsAny<ImportProfile>()), Times.Never);
        }

        // OrganizationController

        [Fact]
        public async Task GetOrganizationInfo_Returns_Unauthorized_For_Anonymous_User()
        {
            var controller = CreateOrganizationController(Anonymous());

            var result = await controller.GetOrganizationInfo(OtherOrganizationId);

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetOrganizationInfo_Returns_Unauthorized_When_Member_Requests_Another()
        {
            var controller = CreateOrganizationController(OrganizationMember());

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

        private Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, AuthorizationInfo authorizationInfo)
        {
            return CreateAuthorizationService().AuthorizeAsync(user, authorizationInfo, new ImportAuthorizationRequirement(ModuleConstants.Security.Permissions.Access));
        }

        private ImportController CreateImportController(ClaimsPrincipal user)
        {
            return new ImportController(
                Mock.Of<IDataImporterRegistrar>(),
                _importRunService.Object,
                _profilesSearchService.Object,
                _historySearchService.Object,
                Mock.Of<IImportProfileCrudService>(),
                CreateAuthorizationService(),
                _dataImporterFactory.Object)
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

        private static ClaimsPrincipal Anonymous(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        private static ClaimsPrincipal Authenticated(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
        }

        private static ClaimsPrincipal WithAccessPermission()
        {
            return Authenticated(new Claim(PlatformConstants.Security.Claims.PermissionClaimType, ModuleConstants.Security.Permissions.Access));
        }

        private static ClaimsPrincipal OrganizationMember()
        {
            return Authenticated(new Claim(PlatformConstants.Security.Claims.MemberIdClaimType, EmployeeId));
        }
    }
}
