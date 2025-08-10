using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TodoApp.Data;
using TodoApp.Infrastructure;
using TodoApp.Model;
using TodoApp.Services;

namespace TodoApp.Tests.Service
{
    [TestClass]
    public class TodoServiceTests
    {
        private class FakeRepository : ITaskRepository
        {
            public bool ThrowOnLoad { get; set; }
            public bool ThrowOnSave { get; set; }
            public List<TodoTask> Tasks { get; set; } = new();
            private int _nextId = 1;

            public void LoadData()
            {
                if (ThrowOnLoad)
                    throw new Exception("load failure");

                if (Tasks.Count > 0)
                    _nextId = Tasks.Max(t => t.Id) + 1;
            }

            public void Add(TodoTask task)
            {
                if (task.Id != default)
                    throw new InvalidOperationException("New tasks should not be assigned ids");
                task.Id = _nextId++;
                Tasks.Add(task);
            }

            public TodoTask Get(int id)
            {
                return Tasks.Single(t => t.Id == id);
            }

            public IEnumerable<TodoTask> GetAll() => Tasks;

            public void Delete(TodoTask task)
            {
                if (!Tasks.Contains(task))
                    throw new InvalidOperationException("Could not delete task that does not exist");
                Tasks.Remove(task);
            }

            public void Save()
            {
                if (ThrowOnSave)
                    throw new Exception("save failure");
                // no-op for in-memory
            }
        }

        private static TodoService CreateService(FakeRepository repo) =>
            new TodoService(repo, new TodoStatusParser());

        [TestMethod]
        public void Ctor_LoadFailure_WrappedAsPersistenceException()
        {
            var repo = new FakeRepository { ThrowOnLoad = true };
            var ex = Assert.ThrowsException<PersistenceException>(() => CreateService(repo));
            StringAssert.StartsWith(ex.Message, "Error loading tasks:");
        }

        [TestMethod]
        public void AddTask_Valid_AddsAndAssignsId()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);

            var t = svc.AddTask("A", "x", "Todo", "d");

            Assert.AreEqual(1, t.Id);
            Assert.AreEqual(1, repo.Tasks.Count);
            Assert.AreEqual("A", repo.Tasks[0].Name);
            Assert.AreEqual(TodoTaskStatus.Todo, repo.Tasks[0].Status);
        }

        [TestMethod]
        public void AddTask_InvalidStatus_ThrowsValidation()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);

            var ex = Assert.ThrowsException<TodoValidationException>(() => svc.AddTask("A", "x", "NotAStatus", "d"));
            Assert.AreEqual("Error: Invalid status. Allowed values: Todo, In Progress, Complete", ex.Message);
        }

        [TestMethod]
        public void ListTasks_StatusFilter_Invalid_ReturnsEmpty()
        {
            var repo = new FakeRepository
            {
                Tasks = new List<TodoTask>
                {
                    new TodoTask { Id = 1, Name = "A", Owner = "o1", Status = TodoTaskStatus.Todo },
                    new TodoTask { Id = 2, Name = "B", Owner = "o2", Status = TodoTaskStatus.InProgress },
                }
            };
            var svc = CreateService(repo);

            var result = svc.ListTasks("not a real status", null);

            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void ListTasks_OwnerFilter_CaseInsensitive()
        {
            var repo = new FakeRepository
            {
                Tasks = new List<TodoTask>
                {
                    new TodoTask { Id = 1, Name = "X", Owner = "John", Status = TodoTaskStatus.Todo },
                    new TodoTask { Id = 2, Name = "Y", Owner = "alice", Status = TodoTaskStatus.Todo },
                }
            };
            var svc = CreateService(repo);

            var result = svc.ListTasks(null, "john").ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("X", result[0].Name);
        }

        [TestMethod]
        public void UpdateTask_NotFound_ThrowsNotFound()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);

            var ex = Assert.ThrowsException<NotFoundException>(() => svc.UpdateTask(999, "Nope", null, null, null));
            Assert.AreEqual("Error: Task with ID 999 not found", ex.Message);
        }

        [TestMethod]
        public void UpdateTask_NoFieldsProvided_ReturnsFalse()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);
            var t = svc.AddTask("A", "x", "Todo", "d");

            var changed = svc.UpdateTask(t.Id, null, null, null, null);

            Assert.IsFalse(changed);
        }

        [TestMethod]
        public void UpdateTask_StatusInvalid_ThrowsValidation()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);
            var t = svc.AddTask("A", "x", "Todo", "d");

            var ex = Assert.ThrowsException<TodoValidationException>(() => svc.UpdateTask(t.Id, null, null, "NotAStatus", null));
            Assert.AreEqual("Error: Invalid status. Allowed values: Todo, In Progress, Complete", ex.Message);
        }

        [TestMethod]
        public void UpdateTask_Valid_UpdatesAndSaves()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);
            var t = svc.AddTask("Base", "o", "Todo", "d");

            var changed = svc.UpdateTask(t.Id, "NewName", "neo", "Complete", "ND");

            Assert.IsTrue(changed);
            var updated = repo.Tasks.Single(x => x.Id == t.Id);
            Assert.AreEqual("NewName", updated.Name);
            Assert.AreEqual("neo", updated.Owner);
            Assert.AreEqual(TodoTaskStatus.Complete, updated.Status);
            Assert.AreEqual("ND", updated.Description);
        }

        [TestMethod]
        public void DeleteTask_NotFound_ThrowsNotFound()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);

            var ex = Assert.ThrowsException<NotFoundException>(() => svc.DeleteTask(1));
            Assert.AreEqual("Error: Task with ID 1 not found", ex.Message);
        }

        [TestMethod]
        public void DeleteTask_DeletesAndSaves()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);
            var t1 = svc.AddTask("A", "x", "Todo", "d");
            var t2 = svc.AddTask("B", "y", "Todo", "d2");

            var deleted = svc.DeleteTask(t1.Id);

            Assert.AreEqual("A", deleted.Name);
            Assert.IsFalse(repo.Tasks.Any(x => x.Id == t1.Id));
            Assert.IsTrue(repo.Tasks.Any(x => x.Id == t2.Id));
        }

        [TestMethod]
        public void CompleteTask_SetsComplete_AndSaves()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);
            var t = svc.AddTask("DoIt", "x", "Todo", "d");

            var completed = svc.CompleteTask(t.Id);

            Assert.AreEqual(TodoTaskStatus.Complete, completed.Status);
            Assert.AreEqual(TodoTaskStatus.Complete, repo.Tasks.Single(x => x.Id == t.Id).Status);
        }

        [TestMethod]
        public void AssignOwner_SetsOwner_AndSaves()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);
            var t = svc.AddTask("Own", null, "Todo", null);

            var assigned = svc.AssignOwner(t.Id, "jane");

            Assert.AreEqual("jane", assigned.Owner);
            Assert.AreEqual("jane", repo.Tasks.Single(x => x.Id == t.Id).Owner);
        }

        [TestMethod]
        public void Add_Update_Delete_SaveFailure_WrappedAsPersistenceException()
        {
            var repo = new FakeRepository();
            var svc = CreateService(repo);

            // Add failure
            repo.ThrowOnSave = true;
            var ex1 = Assert.ThrowsException<PersistenceException>(() => svc.AddTask("A", "x", "Todo", "d"));
            StringAssert.StartsWith(ex1.Message, "Error saving task:");

            // Reset throw for seeding, then cause failures on update/delete
            repo.ThrowOnSave = false;
            var t = svc.AddTask("B", "y", "Todo", "d");

            repo.ThrowOnSave = true;
            var ex2 = Assert.ThrowsException<PersistenceException>(() => svc.UpdateTask(t.Id, "New", null, null, null));
            StringAssert.StartsWith(ex2.Message, "Error saving task:");

            var ex3 = Assert.ThrowsException<PersistenceException>(() => svc.DeleteTask(t.Id));
            StringAssert.StartsWith(ex3.Message, "Error saving task:");
        }
    }
}
