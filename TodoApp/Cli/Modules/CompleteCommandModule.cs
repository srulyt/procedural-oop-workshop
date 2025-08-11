using System;
using System.CommandLine;
using TodoApp.Services;

namespace TodoApp.Cli
{
    public class CompleteCommandModule : ITodoCommandModule
    {
        public Command Build(Func<ITodoService> serviceFactory)
        {
            var cmd = new Command("complete", "Mark a task as complete");

            var id = new Option<int>("--id", "Task ID") { IsRequired = true };
            cmd.AddOption(id);

            cmd.SetHandler((int i) =>
            {
                var svc = serviceFactory();
                var taskToComplete = svc.CompleteTask(i);
                string taskName = taskToComplete.Name ?? "";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task '{taskName}' (ID: {i}) marked as complete");
                Console.ResetColor();
            }, id);

            return cmd;
        }
    }
}
