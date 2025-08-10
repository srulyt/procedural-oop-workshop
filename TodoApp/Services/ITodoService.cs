using System.Collections.Generic;
using TodoApp.Model;

namespace TodoApp.Services
{
    public interface ITodoService
    {
        TodoTask AddTask(string name, string? owner, string? status, string? description);
        IEnumerable<TodoTask> ListTasks(string? statusFilter, string? ownerFilter);
        bool UpdateTask(int id, string? name, string? owner, string? status, string? description);
        TodoTask DeleteTask(int id);
        TodoTask CompleteTask(int id);
        TodoTask AssignOwner(int id, string owner);
    }
}
