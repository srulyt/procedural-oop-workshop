using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TodoApp.Tests.Helpers;

namespace TodoApp.Tests
{
    [TestClass]
    public class ListHeaderDisplayTests
    {
        [TestInitialize]
        public void Setup() => DataSandbox.ClearTasks();

        [TestMethod]
        public void List_Header_RespectsDisplayAttributes_OrderAndName()
        {
            // Seed a couple of tasks
            ConsoleTestHost.RunMain("add", "--name", "A1");
            ConsoleTestHost.RunMain("add", "--name", "A2", "--owner", "bob", "--status", "In Progress");

            var (stdout, _) = ConsoleTestHost.RunMain("list");

            // Find the header line (first non-empty line that contains all expected headers)
            var lines = stdout.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var header = lines.FirstOrDefault(l =>
                !string.IsNullOrWhiteSpace(l) &&
                l.Contains("ID") &&
                l.Contains("Name") &&
                l.Contains("Owner") &&
                l.Contains("Status") &&
                l.Contains("Description"));

            Assert.IsNotNull(header, "Could not find header line in list output.");

            // Ensure order: ID, Name, Owner, Status, Description
            int idxId = header.IndexOf("ID", StringComparison.Ordinal);
            int idxName = header.IndexOf("Name", StringComparison.Ordinal);
            int idxOwner = header.IndexOf("Owner", StringComparison.Ordinal);
            int idxStatus = header.IndexOf("Status", StringComparison.Ordinal);
            int idxDesc = header.IndexOf("Description", StringComparison.Ordinal);

            Assert.IsTrue(idxId >= 0 && idxId < idxName, "ID should appear before Name");
            Assert.IsTrue(idxName >= 0 && idxName < idxOwner, "Name should appear before Owner");
            Assert.IsTrue(idxOwner >= 0 && idxOwner < idxStatus, "Owner should appear before Status");
            Assert.IsTrue(idxStatus >= 0 && idxStatus < idxDesc, "Status should appear before Description");

            // Header should use "ID" (from Display attribute)
            StringAssert.Contains(header, "ID");
        }
    }
}
