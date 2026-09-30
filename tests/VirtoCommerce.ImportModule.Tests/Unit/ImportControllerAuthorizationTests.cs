using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Models.Search;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Web.Controllers.Api;
using VirtoCommerce.Platform.Security.Authorization;
using Xunit;
using ModuleConstants = VirtoCommerce.ImportModule.Core.ModuleConstants;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportControllerAuthorizationTests
    {
        private const string ImporterType = "TestImporter";
        private const string ProfileId = "profile-1";
        private const string JobId = "job-1";
        private const string RunHistoryId = "run-1";

        [Fact]
        public async Task Run_Authorizes_Against_The_Importer_Requirement_When_It_Declares_One()
        {
            var importerRequirement = new PermissionAuthorizationRequirement("custom:import");
            var fixture = new Fixture(importerRequirement, AuthorizationResult.Success());

            await fixture.Controller.RunImport(CreateProfile());

            Assert.Same(importerRequirement, fixture.AuthorizedRequirement);
        }

        [Fact]
        public async Task Run_Requires_Import_Execute_When_The_Importer_Declares_No_Requirement()
        {
            var fixture = new Fixture(null, AuthorizationResult.Success());

            await fixture.Controller.RunImport(CreateProfile());

            var requirement = Assert.IsType<PermissionAuthorizationRequirement>(fixture.AuthorizedRequirement);
            Assert.Equal(ModuleConstants.Security.Permissions.Execute, requirement.Permission);
        }

        [Fact]
        public async Task Run_Refuses_And_Enqueues_Nothing_When_Authorization_Fails()
        {
            var fixture = new Fixture(null, AuthorizationResult.Failed());

            var result = await fixture.Controller.RunImport(CreateProfile());

            Assert.IsType<UnauthorizedResult>(result.Result);
            fixture.RunService.Verify(x => x.RunImportBackgroundJob(It.IsAny<ImportProfile>()), Times.Never);
        }

        [Fact]
        public async Task Resume_Authorizes_Against_The_Importer_Requirement_When_It_Declares_One()
        {
            var importerRequirement = new PermissionAuthorizationRequirement("custom:import");
            var fixture = new Fixture(importerRequirement, AuthorizationResult.Success());

            await fixture.Controller.ResumeImport(new ImportResumeRequest { JobId = JobId });

            Assert.Same(importerRequirement, fixture.AuthorizedRequirement);
        }

        [Fact]
        public async Task Resume_Requires_Import_Execute_When_The_Importer_Declares_No_Requirement()
        {
            var fixture = new Fixture(null, AuthorizationResult.Success());

            await fixture.Controller.ResumeImport(new ImportResumeRequest { JobId = JobId });

            var requirement = Assert.IsType<PermissionAuthorizationRequirement>(fixture.AuthorizedRequirement);
            Assert.Equal(ModuleConstants.Security.Permissions.Execute, requirement.Permission);
        }

        [Fact]
        public async Task Resume_Refuses_And_Resumes_Nothing_When_Authorization_Fails()
        {
            var fixture = new Fixture(null, AuthorizationResult.Failed());

            var result = await fixture.Controller.ResumeImport(new ImportResumeRequest { JobId = JobId });

            Assert.IsType<UnauthorizedResult>(result.Result);
            fixture.RunService.Verify(x => x.ResumeImportAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Run_And_Resume_Carry_No_Static_Permission_Policy()
        {
            var actions = new[] { nameof(ImportController.RunImport), nameof(ImportController.ResumeImport) };

            foreach (var action in actions)
            {
                var attributes = typeof(ImportController).GetMethod(action)!.GetCustomAttributes<AuthorizeAttribute>();

                Assert.DoesNotContain(attributes, x => !string.IsNullOrEmpty(x.Policy));
            }
        }

        private static ImportProfile CreateProfile()
        {
            return new ImportProfile { Id = ProfileId, DataImporterType = ImporterType };
        }

        private sealed class Fixture
        {
            public Fixture(IAuthorizationRequirement importerRequirement, AuthorizationResult authorizationResult)
            {
                var importer = new Mock<IDataImporter>();
                importer.SetupGet(x => x.AuthorizationRequirement).Returns(importerRequirement);

                var importerFactory = new Mock<IDataImporterFactory>();
                importerFactory.Setup(x => x.Create(ImporterType)).Returns(importer.Object);

                var authorizationService = new Mock<IAuthorizationService>();
                authorizationService
                    .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                    .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((_, _, requirements) => AuthorizedRequirement = requirements.Single())
                    .ReturnsAsync(authorizationResult);

                RunService = new Mock<IImportRunService>();
                RunService.Setup(x => x.RunImportBackgroundJob(It.IsAny<ImportProfile>())).Returns(new ImportPushNotification("test"));
                RunService.Setup(x => x.ResumeImportAsync(RunHistoryId)).ReturnsAsync(new ImportPushNotification("test"));

                var runHistorySearchService = new Mock<IImportRunHistorySearchService>();
                runHistorySearchService
                    .Setup(x => x.SearchAsync(It.IsAny<SearchImportRunHistoryCriteria>(), It.IsAny<bool>()))
                    .ReturnsAsync(new SearchImportRunHistoryResult
                    {
                        TotalCount = 1,
                        Results = [new ImportRunHistory { Id = RunHistoryId, JobId = JobId, ProfileId = ProfileId }],
                    });

                var profileCrudService = new Mock<IImportProfileCrudService>();
                profileCrudService
                    .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .ReturnsAsync([CreateProfile()]);

                Controller = new ImportController(
                    Mock.Of<IDataImporterRegistrar>(),
                    RunService.Object,
                    Mock.Of<IImportProfilesSearchService>(),
                    runHistorySearchService.Object,
                    profileCrudService.Object,
                    authorizationService.Object,
                    importerFactory.Object)
                {
                    ControllerContext = new ControllerContext
                    {
                        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) },
                    },
                };
            }

            public ImportController Controller { get; }

            public Mock<IImportRunService> RunService { get; }

            public IAuthorizationRequirement AuthorizedRequirement { get; private set; }
        }
    }
}
