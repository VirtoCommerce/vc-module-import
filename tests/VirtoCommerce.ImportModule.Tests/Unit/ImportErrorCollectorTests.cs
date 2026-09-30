using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Data.Services;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportErrorCollectorTests
    {
        [Fact]
        public void New_Error_Precedes_Seeded_Entries_Which_Render_Exactly_As_Stored()
        {
            var progress = new ImportProgressInfo();
            var collector = new ImportErrorCollector(10, progress, NullLogger.Instance);
            collector.Seed(["Line 5: b", "Line 3: a"]);

            collector.Handle(new ErrorInfo { ErrorLine = 7, ErrorMessage = "c" });

            Assert.Equal(["Line 7: c", "Line 5: b", "Line 3: a"], progress.Errors.ToList());
        }

        [Fact]
        public void Seeded_Entries_Do_Not_Count_Toward_The_Threshold()
        {
            var progress = new ImportProgressInfo();
            var collector = new ImportErrorCollector(2, progress, NullLogger.Instance);
            collector.Seed(["Line 2: x", "Line 1: y", "Line 0: z"]);

            Assert.False(collector.LimitReached);
            collector.Handle(new ErrorInfo { ErrorLine = 3, ErrorMessage = "n1" });
            Assert.False(collector.LimitReached);
            collector.Handle(new ErrorInfo { ErrorLine = 4, ErrorMessage = "n2" });
            Assert.True(collector.LimitReached);
        }

        [Fact]
        public void Limit_Message_Stays_Last_On_Every_Error_After_The_Limit()
        {
            var progress = new ImportProgressInfo();
            var collector = new ImportErrorCollector(2, progress, NullLogger.Instance);

            collector.Handle(new ErrorInfo { ErrorLine = 1, ErrorMessage = "a" });
            collector.Handle(new ErrorInfo { ErrorLine = 2, ErrorMessage = "b" });
            Assert.Equal(ImportErrorCollector.LimitReachedMessage, progress.Errors.Last());
            collector.Handle(new ErrorInfo { ErrorLine = 3, ErrorMessage = "c" });

            Assert.Equal(ImportErrorCollector.LimitReachedMessage, progress.Errors.Last());
            Assert.Equal(1, progress.Errors.Count(x => x == ImportErrorCollector.LimitReachedMessage));
        }

        [Fact]
        public void Seeding_Removes_The_Limit_Message_Wherever_It_Sits_And_Keeps_Every_Other_Entry()
        {
            var progress = new ImportProgressInfo();
            var collector = new ImportErrorCollector(10, progress, NullLogger.Instance);

            collector.Seed(["System.Exception: boom", ImportErrorCollector.LimitReachedMessage, "Line 2: b", "Line 1: a"]);

            Assert.Equal(["System.Exception: boom", "Line 2: b", "Line 1: a"], progress.Errors.ToList());
        }
    }
}
