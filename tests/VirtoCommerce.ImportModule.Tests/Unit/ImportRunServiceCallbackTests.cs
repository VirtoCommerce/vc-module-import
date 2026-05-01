using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportRunServiceCallbackTests
    {
        [Fact]
        public async Task Saves_Cursor_And_PC_When_ShouldSaveHistory_Is_True()
        {
            // Arrange
            var historyCrud = new Mock<IImportRunHistoryCrudService>();
            ImportRunHistory captured = null;
            historyCrud
                .Setup(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()))
                .Callback<IList<ImportRunHistory>>(items =>
                {
                    foreach (var item in items)
                    {
                        captured = item;
                    }
                })
                .Returns(Task.CompletedTask);

            var service = TestHelperFactory.CreateTestableRunService(historyCrud.Object, out var history, out var notif);

            var progress = new ImportProgressInfo
            {
                ProcessedCount = 42,
                TotalCount = 100,
                Cursor = "cursor-payload",
                ShouldSaveHistory = true,
            };

            // Act
            await service.InvokeCallbackForTesting(progress, notif, history);

            // Assert
            historyCrud.Verify(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()), Times.Once);
            Assert.NotNull(captured);
            Assert.Equal("cursor-payload", captured.Cursor);
            Assert.Equal(42, captured.ProcessedCount);
        }

        [Fact]
        public async Task Does_Not_Save_When_ShouldSaveHistory_False()
        {
            // Arrange
            var historyCrud = new Mock<IImportRunHistoryCrudService>();
            historyCrud
                .Setup(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()))
                .Returns(Task.CompletedTask);

            var service = TestHelperFactory.CreateTestableRunService(historyCrud.Object, out var history, out var notif);

            var progress = new ImportProgressInfo
            {
                ProcessedCount = 5,
                TotalCount = 100,
                Cursor = "irrelevant",
                ShouldSaveHistory = false,
            };

            // Act
            await service.InvokeCallbackForTesting(progress, notif, history);

            // Assert
            historyCrud.Verify(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()), Times.Never);
        }

        [Fact]
        public async Task Save_Exception_Is_Swallowed_And_Logged()
        {
            // Arrange
            var historyCrud = new Mock<IImportRunHistoryCrudService>();
            historyCrud
                .Setup(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()))
                .ThrowsAsync(new InvalidOperationException("db is down"));

            var service = TestHelperFactory.CreateTestableRunService(historyCrud.Object, out var history, out var notif);

            var progress = new ImportProgressInfo
            {
                ProcessedCount = 10,
                Cursor = "cursor-payload",
                ShouldSaveHistory = true,
            };

            // Act
            var exception = await Record.ExceptionAsync(() => service.InvokeCallbackForTesting(progress, notif, history));

            // Assert
            Assert.Null(exception);
            historyCrud.Verify(x => x.SaveChangesAsync(It.IsAny<IList<ImportRunHistory>>()), Times.Once);
        }
    }
}
