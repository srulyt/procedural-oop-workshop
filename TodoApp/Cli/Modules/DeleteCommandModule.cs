using System;
using System.CommandLine;
using TodoApp.Services;

namespace TodoApp.Cli
{
    public class DeleteCommandModule : ITodoCommandModule
    {
        public Command Build(Func<ITodoService> serviceFactory)
        {
            var cmd = new Command("delete", "Delete a task");

            var id = new Option<int>("--id", "Task ID") { IsRequired = true };
            cmd.AddOption(id);

            cmd.SetHandler((int i) =>
            {
                var svc = serviceFactory();
                var taskToDelete = svc.DeleteTask(i);
                string taskName = taskToDelete.Name ?? "";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task '{taskName}' (ID: {i}) deleted successfully");
                Console.ResetColor();
            }, id);

            return cmd;
        }
    }
}
