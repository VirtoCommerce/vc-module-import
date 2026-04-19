using Microsoft.Extensions.Logging.Abstractions;
using VirtoCommerce.ImportModule.Data.Services;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportRunServiceRequeueTests
    {
        [Fact]
        public void RequeueImportBackgroundJob_delegates_to_Hangfire_wrapper()
        {
            // Arrange
            var service = new TestableRequeueService();

            // Act
            var result = service.RequeueImportBackgroundJob("abc");

            // Assert
            Assert.Equal("abc", service.RequeuedJobId);
            Assert.True(result);
        }

        private sealed class TestableRequeueService : ImportRunService
        {
            public TestableRequeueService()
                : base(
                    /* UserManager */                 null!,
                    /* IUserNameResolver */           null!,
                    /* IMemberService */              null!,
                    /* IBackgroundJobExecutor */      null!,
                    /* IPushNotificationManager */    null!,
                    /* INotificationSearchService */  null!,
                    /* INotificationSender */         null!,
                    /* IImportProfileCrudService */   null!,
                    /* IImportRunHistoryCrudService */ null!,
                    /* IDataImporterFactory */        null!,
                    /* IDataImportProcessManager */   null!,
                    /* ILogger<ImportRunService> */   NullLogger<ImportRunService>.Instance)
            {
            }

            public string RequeuedJobId { get; private set; }

            protected override bool RequeueHangfireJob(string jobId)
            {
                RequeuedJobId = jobId;
                return true;
            }
        }
    }
}
