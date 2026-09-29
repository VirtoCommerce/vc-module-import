using Newtonsoft.Json;
using VirtoCommerce.ImportModule.Core.Models;
using Xunit;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportProfileSerializationTests
    {
        [Fact]
        public void Serialized_Profile_Carries_No_Run_History()   // U20, out
        {
            var json = JsonConvert.SerializeObject(new ImportProfile { Name = "p", RunHistory = new ImportRunHistory { Id = "H1", Cursor = "c" } });

            Assert.DoesNotContain("RunHistory", json);
        }

        [Fact]
        public void Posted_Run_History_Is_Not_Bound()   // U20, in
        {
            var profile = JsonConvert.DeserializeObject<ImportProfile>("{\"Name\":\"p\",\"RunHistory\":{\"Id\":\"H1\",\"Cursor\":\"c\",\"Errors\":[\"planted\"]}}");

            Assert.Null(profile.RunHistory);
        }
    }
}
