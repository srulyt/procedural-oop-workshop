using Microsoft.VisualStudio.TestTools.UnitTesting;
using TodoApp.Tests.Helpers;

namespace TodoApp.Tests
{
    [TestClass]
    public class DuplicateIdBehaviorTests
    {
        [TestInitialize]
        public void Setup() => DataSandbox.ClearTasks();

        [TestMethod]
        public void DuplicateIds_Update_ShowsFriendlyNotFound_And_ListStillWorks()
        {
            // Seed two tasks with the same Id to simulate corrupt/duplicate data
            DataSandbox.SeedJson(@"[
  { ""Id"": 1, ""Name"": ""A"", ""Owner"": ""x"", ""Status"": ""Todo"", ""Description"": ""d"" },
  { ""Id"": 1, ""Name"": ""B"", ""Owner"": ""y"", ""Status"": ""Todo"", ""Description"": ""d2"" }
]");

            // Attempting to update should surface a friendly error (repository throws due to duplicates)
            var (updOut, _) = ConsoleTestHost.RunMain("update", "--id", "1", "--name", "C");
            StringAssert.Contains(updOut, "Error: Task with ID 1 not found");

            // Listing should still work and show both original tasks (unchanged)
            var (listOut, _) = ConsoleTestHost.RunMain("list");
            StringAssert.Contains(listOut, "A");
            StringAssert.Contains(listOut, "B");
        }
    }
}
