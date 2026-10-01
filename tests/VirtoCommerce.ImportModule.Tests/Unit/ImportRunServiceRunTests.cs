using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.PushNotifications;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.ImportModule.Data.Services;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportRunServiceRunTests
    {
        [Fact]
        public async Task Failure_Before_The_First_Progress_Keeps_The_Stored_Errors_Of_A_Resumed_Run()
        {
            var row = new ImportRunHistory
            {
                Id = "H1",
                Cursor = "dummy-cursor",
                Errors = new List<string> { "Line 5: b", "Line 3: a" },
                ErrorsCount = 2,
            };
            var profile = new ImportProfile { RunHistory = row };
            List<string> savedErrors = null;
            var historyCrud = new Mock<IImportRunHistoryCrudService>();
            historyCrud.Setup(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()))
                .Callback<IList<ImportRunHistory>>(x => savedErrors = x[0].Errors?.ToList())
                .Returns(Task.CompletedTask);
            var manager = new Mock<IDataImportProcessManager>();
            manager.Setup(x => x.ImportAsync(It.IsAny<ImportProfile>(), It.IsAny<Func<ImportProgressInfo, Task>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("opening failed"));
            var service = new TestableRunImportService(historyCrud.Object, manager.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RunImportAsync(profile, new ImportPushNotification("tester") { JobId = "job-1" }, CancellationToken.None));

            // The row saved in finally is the one the failed run leaves behind
            Assert.Contains("Line 5: b", savedErrors);
            Assert.Contains("Line 3: a", savedErrors);
            Assert.Contains(savedErrors, x => x.Contains("opening failed"));
        }

        private sealed class TestableRunImportService : ImportRunService
        {
            public TestableRunImportService(IImportRunHistoryCrudService historyCrud, IDataImportProcessManager manager)
                : base(
                    /* UserManager */                    BuildUserManager(),
                    /* IUserNameResolver */              null!,
                    /* IMemberService */                 null!,
                    /* IBackgroundJobExecutor */         null!,
                    /* IPushNotificationManager */       BuildPushNotificationManager(),
                    /* INotificationSearchService */     null!,
                    /* INotificationSender */            null!,
                    /* IImportProfileCrudService */      null!,
                    /* IImportRunHistoryCrudService */   historyCrud,
                    /* IImportRunHistorySearchService */ null!,
                    /* IDataImporterFactory */           null!,
                    /* IDataImportProcessManager */      manager,
                    /* ILogger<ImportRunService> */      NullLogger<ImportRunService>.Instance)
            {
            }

            // The finally block looks the creator up; no user means no completion e-mail.
            private static UserManager<ApplicationUser> BuildUserManager()
            {
                var userManager = new Mock<UserManager<ApplicationUser>>(
                    Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
                userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

                return userManager.Object;
            }

            private static IPushNotificationManager BuildPushNotificationManager()
            {
                var pushManager = new Mock<IPushNotificationManager>();
                pushManager.Setup(x => x.SendAsync(It.IsAny<PushNotification>())).Returns(Task.CompletedTask);

                return pushManager.Object;
            }
        }
    }
}
