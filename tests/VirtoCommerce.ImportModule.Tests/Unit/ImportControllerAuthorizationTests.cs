using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ImportModule.Web.Controllers.Api;
using Xunit;
using ModuleConstants = VirtoCommerce.ImportModule.Core.ModuleConstants;

namespace VirtoCommerce.ImportModule.Tests.Unit
{
    public class ImportControllerAuthorizationTests
    {
        [Theory]
        [InlineData(nameof(ImportController.ResumeImport))]   // U15
        [InlineData(nameof(ImportController.RunImport))]      // U16
        public void Action_Requires_Import_Access(string action)
        {
            var attribute = typeof(ImportController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

            Assert.Equal(ModuleConstants.Security.Permissions.Access, attribute?.Policy);
        }
    }
}
