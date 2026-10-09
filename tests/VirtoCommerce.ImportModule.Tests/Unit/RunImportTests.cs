using System;
using System.Collections.Generic;
using System.Linq;
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
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.ImportModule.Web.Authorization;
using VirtoCommerce.ImportModule.Web.Controllers.Api;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Security.Authorization;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    /// <summary>
    /// Tests for <see cref="ImportController.RunImport"/> and <see cref="ImportController.ResumeImport"/> with importers
    /// registered through the real <see cref="DataImporterRegistrar"/>, the platform permission handler and
    /// <see cref="ImportAuthorizationHandler"/>.
    /// </summary>
    public class RunImportTests
    {
        private const string ImporterPermission = "test:run-import";
        private const string OrganizationId = "org-1";
        private const string OtherOrganizationId = "org-2";
        private const string EmployeeId = "employee-1";
        private const string JobId = "job-1";

        private static readonly string _access = ModuleConstants.Security.Permissions.Access;
        private static readonly string _execute = ModuleConstants.Security.Permissions.Execute;
        private static readonly DataImporterRegistrar _registrar = CreateRegistrar();

        private readonly Mock<IMemberResolver> _memberResolver = new();
        private readonly Mock<IImportRunService> _importRunService = new();
        private readonly Mock<IImportRunHistorySearchService> _historySearchService = new();
        private readonly Mock<IImportProfileCrudService> _profileCrudService = new();

        public RunImportTests()
        {
            _memberResolver
                .Setup(x => x.ResolveMemberByIdAsync(EmployeeId))
                .ReturnsAsync(new Employee { Id = EmployeeId, Organizations = new List<string> { OrganizationId } });
            _memberResolver
                .Setup(x => x.ResolveMemberByIdAsync(OrganizationId))
                .ReturnsAsync(new Organization { Id = OrganizationId });

            _importRunService
                .Setup(x => x.RunImportBackgroundJob(It.IsAny<ImportProfile>()))
                .Returns(new ImportPushNotification("test") { JobId = JobId });
            _importRunService
                .Setup(x => x.ResumeImportAsync(It.IsAny<string>()))
                .ReturnsAsync(new ImportPushNotification("test") { JobId = JobId });
        }

        // Run: importer without its own requirement

        [Fact]
        public async Task Run_Anonymous_User_Is_Unauthorized()
        {
            var result = await RunAsync(Anonymous(), nameof(RunTestImporter));

            AssertUnauthorizedAndNotEnqueued(result);
        }

        [Fact]
        public async Task Run_Authenticated_User_Without_Permission_Is_Unauthorized()
        {
            var result = await RunAsync(Authenticated(), nameof(RunTestImporter));

            AssertUnauthorizedAndNotEnqueued(result);
        }

        [Fact]
        public async Task Run_Organization_Member_Without_Permission_Is_Unauthorized()
        {
            var result = await RunAsync(OrganizationMember(), nameof(RunTestImporter));

            AssertUnauthorizedAndNotEnqueued(result);
        }

        [Fact]
        public async Task Run_Access_Permission_Alone_Is_Not_Enough()
        {
            var result = await RunAsync(WithPermissions(_access), nameof(RunTestImporter));

            AssertUnauthorizedAndNotEnqueued(result);
        }

        [Fact]
        public async Task Run_Execute_Permission_Alone_Is_Not_Enough_Without_Access_Permission()
        {
            var result = await RunAsync(WithPermissions(_execute), nameof(RunTestImporter));

            AssertUnauthorizedAndNotEnqueued(result);
        }

        [Fact]
        public async Task Run_User_With_Access_And_Execute_Permissions_Can_Run()
        {
            var result = await RunAsync(WithPermissions(_access, _execute), nameof(RunTestImporter));

            AssertOkAndEnqueued(result);
        }

        [Fact]
        public async Task Run_Organization_Member_With_Access_And_Execute_Permissions_Can_Run()
        {
            var result = await RunAsync(OrganizationMember(_access, _execute), nameof(RunTestImporter));

            AssertOkAndEnqueued(result);
        }

        // Run: importer with its own permission requirement

        [Fact]
        public async Task Run_Anonymous_User_Is_Unauthorized_For_Importer_With_Permission_Requirement()
        {
            var result = await RunAsync(Anonymous(), nameof(PermissionRunTestImporter));

            AssertUnauthorizedAndNotEnqueued(result);
        }

        [Fact]
        public async Task Run_Access_Permission_Alone_Is_Not_Enough_For_Importer_With_Own_Permission()
        {
            var result = await RunAsync(WithPermissions(_access), nameof(PermissionRunTestImporter));

            AssertUnauthorizedAndNotEnqueued(result);
        }

        [Fact]
        public async Task Run_Importer_Permission_Alone_Is_Not_Enough_Without_Access_Permission()
        {
            var result = await RunAsync(WithPermissions(ImporterPermission), nameof(PermissionRunTestImporter));

            AssertUnauthorizedAndNotEnqueued(result);
        }

        [Fact]
        public async Task Run_User_With_Access_And_Importer_Permissions_Can_Run_Importer_With_Permission_Requirement()
        {
            var result = await RunAsync(WithPermissions(_access, ImporterPermission), nameof(PermissionRunTestImporter));

            AssertOkAndEnqueued(result);
        }

        // Run: unregistered importer type

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("NoSuchImporter")]
        public async Task Run_Anonymous_User_Is_Unauthorized_For_Unregistered_ImporterType(string importerType)
        {
            var result = await RunAsync(Anonymous(), importerType);

            AssertUnauthorizedAndNotEnqueued(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("NoSuchImporter")]
        public async Task Run_Returns_BadRequest_For_Unregistered_ImporterType(string importerType)
        {
            var result = await RunAsync(WithPermissions(_access), importerType);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            _importRunService.Verify(x => x.RunImportBackgroundJob(It.IsAny<ImportProfile>()), Times.Never);
        }

        // Run: request body

        [Fact]
        public async Task Run_Organization_Member_Cannot_Run_Import_For_Another_Organization()
        {
            var profile = new ImportProfile { DataImporterType = nameof(RunTestImporter), UserId = OtherOrganizationId };

            await CreateController(OrganizationMember(_access, _execute)).RunImport(profile);

            _importRunService.Verify(x => x.RunImportBackgroundJob(It.Is<ImportProfile>(p => p.UserId == OrganizationId)), Times.Once);
        }

        [Fact]
        public async Task Run_Keeps_UserId_For_User_Without_Organization()
        {
            var profile = new ImportProfile { DataImporterType = nameof(RunTestImporter), UserId = OtherOrganizationId };

            await CreateController(WithPermissions(_access, _execute)).RunImport(profile);

            _importRunService.Verify(x => x.RunImportBackgroundJob(It.Is<ImportProfile>(p => p.UserId == OtherOrganizationId)), Times.Once);
        }

        // Resume

        [Fact]
        public async Task Resume_Anonymous_User_Is_Unauthorized_Before_Run_History_Lookup()
        {
            var result = await CreateController(Anonymous()).ResumeImport(new ImportResumeRequest { JobId = JobId });

            Assert.IsType<UnauthorizedResult>(result.Result);
            _historySearchService.Verify(x => x.SearchAsync(It.IsAny<SearchImportRunHistoryCriteria>(), It.IsAny<bool>()), Times.Never);
            _importRunService.Verify(x => x.ResumeImportAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Resume_Organization_Member_Without_Permission_Is_Unauthorized()
        {
            SetupRunHistory(OrganizationId);

            var result = await CreateController(OrganizationMember()).ResumeImport(new ImportResumeRequest { JobId = JobId });

            Assert.IsType<UnauthorizedResult>(result.Result);
            _importRunService.Verify(x => x.ResumeImportAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Resume_Access_Permission_Alone_Is_Not_Enough()
        {
            SetupRunHistory(OrganizationId);

            var result = await CreateController(OrganizationMember(_access)).ResumeImport(new ImportResumeRequest { JobId = JobId });

            Assert.IsType<UnauthorizedResult>(result.Result);
            _importRunService.Verify(x => x.ResumeImportAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Resume_Organization_Member_Can_Resume_Own_Organization_Run()
        {
            SetupRunHistory(OrganizationId);

            var result = await CreateController(OrganizationMember(_access, _execute)).ResumeImport(new ImportResumeRequest { JobId = JobId });

            Assert.IsType<OkObjectResult>(result.Result);
            _importRunService.Verify(x => x.ResumeImportAsync("run-1"), Times.Once);
        }

        [Fact]
        public async Task Resume_Organization_Member_Gets_Not_Found_For_Another_Organization_Run()
        {
            SetupRunHistory(OtherOrganizationId);

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => CreateController(OrganizationMember(_access)).ResumeImport(new ImportResumeRequest { JobId = JobId }));

            _importRunService.Verify(x => x.ResumeImportAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Resume_User_Without_Organization_Can_Resume_Any_Run()
        {
            SetupRunHistory(OtherOrganizationId);

            var result = await CreateController(WithPermissions(_access, _execute)).ResumeImport(new ImportResumeRequest { JobId = JobId });

            Assert.IsType<OkObjectResult>(result.Result);
            _importRunService.Verify(x => x.ResumeImportAsync("run-1"), Times.Once);
        }

        // Helpers

        private void SetupRunHistory(string organizationId)
        {
            _historySearchService
                .Setup(x => x.SearchAsync(It.IsAny<SearchImportRunHistoryCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new SearchImportRunHistoryResult
                {
                    TotalCount = 1,
                    Results = [new ImportRunHistory { Id = "run-1", JobId = JobId, ProfileId = "profile-1", UserId = organizationId }],
                });
            _profileCrudService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([new ImportProfile { Id = "profile-1", DataImporterType = nameof(RunTestImporter), UserId = organizationId }]);
        }

        private Task<ActionResult<ImportPushNotification>> RunAsync(ClaimsPrincipal user, string importerType)
        {
            return CreateController(user).RunImport(new ImportProfile { DataImporterType = importerType });
        }

        private void AssertUnauthorizedAndNotEnqueued(ActionResult<ImportPushNotification> result)
        {
            Assert.IsType<UnauthorizedResult>(result.Result);
            _importRunService.Verify(x => x.RunImportBackgroundJob(It.IsAny<ImportProfile>()), Times.Never);
        }

        private void AssertOkAndEnqueued(ActionResult<ImportPushNotification> result)
        {
            Assert.IsType<OkObjectResult>(result.Result);
            _importRunService.Verify(x => x.RunImportBackgroundJob(It.IsAny<ImportProfile>()), Times.Once);
        }

        private ImportController CreateController(ClaimsPrincipal user)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorizationCore();
            services.AddSingleton(_memberResolver.Object);
            services.AddSingleton<IAuthorizationHandler, DefaultPermissionAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, ImportAuthorizationHandler>();
            var authorizationService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();

            return new ImportController(
                _registrar,
                _importRunService.Object,
                Mock.Of<IImportProfilesSearchService>(),
                _historySearchService.Object,
                _profileCrudService.Object,
                authorizationService,
                _registrar)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } },
            };
        }

        private static DataImporterRegistrar CreateRegistrar()
        {
            // Registration goes to the static AbstractTypeFactory<IDataImporter>, so it is done once per test run
            var registrar = new DataImporterRegistrar(serviceProvider: null);
            registrar.Register<RunTestImporter>();
            registrar.Register<PermissionRunTestImporter>().WithAuthorizationPermission(ImporterPermission);

            return registrar;
        }

        private static ClaimsPrincipal Anonymous()
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        private static ClaimsPrincipal Authenticated(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
        }

        private static ClaimsPrincipal WithPermissions(params string[] permissions)
        {
            return Authenticated(PermissionClaims(permissions).ToArray());
        }

        private static ClaimsPrincipal OrganizationMember(params string[] permissions)
        {
            return Authenticated(PermissionClaims(permissions)
                .Append(new Claim(PlatformConstants.Security.Claims.MemberIdClaimType, EmployeeId))
                .ToArray());
        }

        private static IEnumerable<Claim> PermissionClaims(IEnumerable<string> permissions)
        {
            return permissions.Select(x => new Claim(PlatformConstants.Security.Claims.PermissionClaimType, x));
        }

        public class RunTestImporter : IDataImporter
        {
            public string TypeName => GetType().Name;
            public Dictionary<string, string> Metadata { get; } = new();
            public SettingDescriptor[] AvailSettings { get; set; }
            public IAuthorizationRequirement AuthorizationRequirement { get; set; }

            public IImportDataReader OpenReader(ImportContext context) => throw new NotSupportedException();
            public IImportDataWriter OpenWriter(ImportContext context) => throw new NotSupportedException();
            public Task<ValidationResult> ValidateAsync(ImportContext context) => Task.FromResult(new ValidationResult());
            public object Clone() => MemberwiseClone();
        }

        public class PermissionRunTestImporter : RunTestImporter
        {
        }
    }
}
