using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TodoApp.Tests.Helpers;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.MethodLevel)]

namespace TodoApp.Tests
{
    [TestClass]
    public class HelpTests
    {
        [TestInitialize]
        public void Setup() => DataSandbox.ClearTasks();

        [TestMethod]
        public void Help_NoArgs_PrintsUsage()
        {
            var (stdout, _) = ConsoleTestHost.RunMain(Array.Empty<string>());
            StringAssert.Contains(stdout, "TodoApp - CLI Task Manager");
            StringAssert.Contains(stdout, "Usage:");
            StringAssert.Contains(stdout, "Commands:");
        }

        [TestMethod]
        public void Help_LongAndShortFlags_PrintsUsage()
        {
            var (o1, _) = ConsoleTestHost.RunMain("--help");
            var (o2, _) = ConsoleTestHost.RunMain("-h");
            StringAssert.Contains(o1, "Usage:");
            StringAssert.Contains(o2, "Usage:");
        }
    }

    [TestClass]
    public class UnknownAndErrorsTests
    {
        [TestInitialize]
        public void Setup() => DataSandbox.ClearTasks();

        [TestMethod]
        public void UnknownCommand_ShowsErrorAndHint()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("foo");
            StringAssert.Contains(stdout, "Error: Unknown command 'foo'");
            StringAssert.Contains(stdout, "Use 'todoapp --help' to see available commands");
        }
    }

    [TestClass]
    public class AddTests
    {
        [TestInitialize]
        public void Setup() => DataSandbox.ClearTasks();

        [TestMethod]
        public void Add_MissingName_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("add");
            StringAssert.Contains(stdout, "Error: --name parameter is required");
        }

        [TestMethod]
        public void Add_Minimal_Succeeds_WithDefaults_And_ListShowsTask()
        {
            var (addOut, _) = ConsoleTestHost.RunMain("add", "--name", "T1");
            StringAssert.Contains(addOut, "Task 'T1' added successfully with ID 1");

            var (listOut, _) = ConsoleTestHost.RunMain("list");
            StringAssert.Contains(listOut, "ID");
            StringAssert.Contains(listOut, "T1");
            StringAssert.Contains(listOut, "Unassigned");
            StringAssert.Contains(listOut, "Todo");
        }

        [TestMethod]
        public void Add_AllFields_Succeeds_And_FilterByOwnerShowsTask()
        {
            var (addOut, _) = ConsoleTestHost.RunMain("add", "--name", "T2", "--owner", "john", "--status", "In Progress", "--description", "D");
            StringAssert.Contains(addOut, "Task 'T2' added successfully");

            var (listOut, _) = ConsoleTestHost.RunMain("list", "--owner", "john");
            StringAssert.Contains(listOut, "T2");
            StringAssert.Contains(listOut, "john");
            StringAssert.Contains(listOut, "In Progress");
            StringAssert.Contains(listOut, "D");
        }

        [TestMethod]
        public void Add_AutoIncrementId_FromExistingData()
        {
            // Seed two existing tasks with Ids 1 and 2
            DataSandbox.SeedJson(@"[
  { ""Id"": 1, ""Name"": ""A"", ""Owner"": ""x"", ""Status"": ""Todo"", ""Description"": ""d"" },
  { ""Id"": 2, ""Name"": ""B"", ""Owner"": ""y"", ""Status"": ""Todo"", ""Description"": ""d2"" }
]");

            var (addOut, _) = ConsoleTestHost.RunMain("add", "--name", "C");
            StringAssert.Contains(addOut, "ID 3");
        }
    }

    [TestClass]
    public class ListTests
    {
        [TestInitialize]
        public void Setup() => DataSandbox.ClearTasks();

        [TestMethod]
        public void List_NoTasks_ShowsFriendlyMessage()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("list");
            StringAssert.Contains(stdout, "No tasks found.");
        }

        [TestMethod]
        public void List_AllTasks_ShowsTableWithHeader()
        {
            ConsoleTestHost.RunMain("add", "--name", "T1");
            ConsoleTestHost.RunMain("add", "--name", "T2", "--owner", "bob", "--status", "In Progress", "--description", "D2");

            var (stdout, _) = ConsoleTestHost.RunMain("list");
            StringAssert.Contains(stdout, "ID");
            StringAssert.Contains(stdout, "Name");
            StringAssert.Contains(stdout, "Owner");
            StringAssert.Contains(stdout, "Status");
            StringAssert.Contains(stdout, "Description");
            StringAssert.Contains(stdout, new string('-', 90));
            StringAssert.Contains(stdout, "T1");
            StringAssert.Contains(stdout, "T2");
        }

        [TestMethod]
        public void List_FilterByStatus_CaseInsensitive()
        {
            ConsoleTestHost.RunMain("add", "--name", "A", "--status", "Todo");
            ConsoleTestHost.RunMain("add", "--name", "B", "--status", "In Progress");

            var (stdout, _) = ConsoleTestHost.RunMain("list", "--status", "in progress");
            StringAssert.Contains(stdout, "B");
            Assert.IsFalse(stdout.Contains("A", StringComparison.Ordinal));
        }

        [TestMethod]
        public void List_FilterByOwner_CaseInsensitive()
        {
            ConsoleTestHost.RunMain("add", "--name", "X", "--owner", "John");
            ConsoleTestHost.RunMain("add", "--name", "Y", "--owner", "alice");

            var (stdout, _) = ConsoleTestHost.RunMain("list", "--owner", "john");
            StringAssert.Contains(stdout, "X");
            Assert.IsFalse(stdout.Contains("Y", StringComparison.Ordinal));
        }

        [TestMethod]
        public void List_TruncatesLongFields_WithEllipsis()
        {
            string longName = new string('N', 40);
            string longOwner = new string('O', 40);
            string longDesc = new string('D', 80);

            ConsoleTestHost.RunMain("add",
                "--name", longName,
                "--owner", longOwner,
                "--description", longDesc);

            var (stdout, _) = ConsoleTestHost.RunMain("list");

            // Expected truncations based on Program.cs logic
            string expName = longName.Substring(0, 22) + "...";
            string expOwner = longOwner.Substring(0, 12) + "...";
            string expDesc = longDesc.Substring(0, 27) + "...";

            StringAssert.Contains(stdout, expName);
            StringAssert.Contains(stdout, expOwner);
            StringAssert.Contains(stdout, expDesc);
        }
    }

    [TestClass]
    public class UpdateTests
    {
        [TestInitialize]
        public void Setup()
        {
            DataSandbox.ClearTasks();
            ConsoleTestHost.RunMain("add", "--name", "Base", "--owner", "o", "--description", "d");
        }

        [TestMethod]
        public void Update_MissingId_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("update");
            StringAssert.Contains(stdout, "Error: --id parameter is required");
        }

        [TestMethod]
        public void Update_InvalidId_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("update", "--id", "X");
            StringAssert.Contains(stdout, "Error: Invalid task ID");
        }

        [TestMethod]
        public void Update_NotFound_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("update", "--id", "999", "--name", "Nope");
            StringAssert.Contains(stdout, "Error: Task with ID 999 not found");
        }

        [TestMethod]
        public void Update_NoProperties_ShowsWarning()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("update", "--id", "1");
            StringAssert.Contains(stdout, "Warning: No properties specified to update");
        }

        [TestMethod]
        public void Update_Properties_Succeeds_And_Persists()
        {
            var (out1, _) = ConsoleTestHost.RunMain("update", "--id", "1", "--name", "NewName", "--owner", "neo", "--status", "Complete", "--description", "ND");
            StringAssert.Contains(out1, "Task 1 updated successfully");

            var (listOut, _) = ConsoleTestHost.RunMain("list");
            StringAssert.Contains(listOut, "NewName");
            StringAssert.Contains(listOut, "neo");
            StringAssert.Contains(listOut, "Complete");
            StringAssert.Contains(listOut, "ND");
        }
    }

    [TestClass]
    public class DeleteTests
    {
        [TestInitialize]
        public void Setup()
        {
            DataSandbox.ClearTasks();
        }

        [TestMethod]
        public void Delete_MissingId_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("delete");
            StringAssert.Contains(stdout, "Error: --id parameter is required");
        }

        [TestMethod]
        public void Delete_InvalidId_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("delete", "--id", "X");
            StringAssert.Contains(stdout, "Error: Invalid task ID");
        }

        [TestMethod]
        public void Delete_NotFound_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("delete", "--id", "1");
            StringAssert.Contains(stdout, "Error: Task with ID 1 not found");
        }

        [TestMethod]
        public void Delete_Succeeds_And_Persists()
        {
            ConsoleTestHost.RunMain("add", "--name", "A");
            ConsoleTestHost.RunMain("add", "--name", "B");

            var (delOut, _) = ConsoleTestHost.RunMain("delete", "--id", "1");
            StringAssert.Contains(delOut, "deleted successfully");

            var (listOut, _) = ConsoleTestHost.RunMain("list");
            Assert.IsFalse(listOut.Contains("A", StringComparison.Ordinal));
            StringAssert.Contains(listOut, "B");
        }
    }

    [TestClass]
    public class CompleteTests
    {
        [TestInitialize]
        public void Setup()
        {
            DataSandbox.ClearTasks();
            ConsoleTestHost.RunMain("add", "--name", "DoIt");
        }

        [TestMethod]
        public void Complete_MissingId_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("complete");
            StringAssert.Contains(stdout, "Error: --id parameter is required");
        }

        [TestMethod]
        public void Complete_InvalidId_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("complete", "--id", "X");
            StringAssert.Contains(stdout, "Error: Invalid task ID");
        }

        [TestMethod]
        public void Complete_NotFound_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("complete", "--id", "42");
            StringAssert.Contains(stdout, "Error: Task with ID 42 not found");
        }

        [TestMethod]
        public void Complete_Succeeds_And_Persists()
        {
            var (out1, _) = ConsoleTestHost.RunMain("complete", "--id", "1");
            StringAssert.Contains(out1, "marked as complete");

            var (listOut, _) = ConsoleTestHost.RunMain("list");
            StringAssert.Contains(listOut, "Complete");
        }
    }

    [TestClass]
    public class AssignTests
    {
        [TestInitialize]
        public void Setup()
        {
            DataSandbox.ClearTasks();
            ConsoleTestHost.RunMain("add", "--name", "Ownable");
        }

        [TestMethod]
        public void Assign_MissingOwnerOrId_ShowsError()
        {
            var (stdout1, _) = ConsoleTestHost.RunMain("assign", "--id", "1");
            StringAssert.Contains(stdout1, "Error: --id and --owner parameters are required");

            var (stdout2, _) = ConsoleTestHost.RunMain("assign", "--owner", "jane");
            StringAssert.Contains(stdout2, "Error: --id and --owner parameters are required");
        }

        [TestMethod]
        public void Assign_InvalidId_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("assign", "--id", "X", "--owner", "jane");
            StringAssert.Contains(stdout, "Error: Invalid task ID");
        }

        [TestMethod]
        public void Assign_NotFound_ShowsError()
        {
            var (stdout, _) = ConsoleTestHost.RunMain("assign", "--id", "2", "--owner", "jane");
            StringAssert.Contains(stdout, "Error: Task with ID 2 not found");
        }

        [TestMethod]
        public void Assign_Succeeds_And_Persists()
        {
            var (out1, _) = ConsoleTestHost.RunMain("assign", "--id", "1", "--owner", "jane");
            StringAssert.Contains(out1, "assigned to jane");

            var (listOut, _) = ConsoleTestHost.RunMain("list", "--owner", "jane");
            StringAssert.Contains(listOut, "Ownable");
        }
    }

    [TestClass]
    public class CorruptJsonTests
    {
        [TestInitialize]
        public void Setup() => DataSandbox.ClearTasks();

        [TestMethod]
        public void CorruptJson_ReportsErrorOnLoad()
        {
            DataSandbox.SeedJson("{ not valid json");

            var (stdout, _) = ConsoleTestHost.RunMain("list");
            StringAssert.Contains(stdout, "Error loading tasks:");
        }
    }

    [TestClass]
    public class StatusValidationTests
    {
        [TestInitialize]
        public void Setup() => DataSandbox.ClearTasks();

        [TestMethod]
        public void Status_Validation_AddAndUpdate()
        {
            // Valid adds with allowed statuses
            var (ok1, _) = ConsoleTestHost.RunMain("add", "--name", "A1", "--status", "Todo");
            StringAssert.Contains(ok1, "added successfully");

            var (ok2, _) = ConsoleTestHost.RunMain("add", "--name", "A2", "--status", "In Progress");
            StringAssert.Contains(ok2, "added successfully");

            var (ok3, _) = ConsoleTestHost.RunMain("add", "--name", "A3", "--status", "Complete");
            StringAssert.Contains(ok3, "added successfully");

            var (listOut, _) = ConsoleTestHost.RunMain("list");
            StringAssert.Contains(listOut, "Todo");
            StringAssert.Contains(listOut, "In Progress");
            StringAssert.Contains(listOut, "Complete");

            // Invalid add status
            var (badAdd, _) = ConsoleTestHost.RunMain("add", "--name", "Bad", "--status", "NotAStatus");
            StringAssert.Contains(badAdd, "Error: Invalid status");

            // Valid update to Complete
            var (updOk, _) = ConsoleTestHost.RunMain("update", "--id", "1", "--status", "Complete");
            StringAssert.Contains(updOk, "updated successfully");
            var (listAfterUpd, _) = ConsoleTestHost.RunMain("list");
            StringAssert.Contains(listAfterUpd, "Complete");

            // Invalid update status
            var (updBad, _) = ConsoleTestHost.RunMain("update", "--id", "2", "--status", "NotAStatus");
            StringAssert.Contains(updBad, "Error: Invalid status");
        }
    }
}
