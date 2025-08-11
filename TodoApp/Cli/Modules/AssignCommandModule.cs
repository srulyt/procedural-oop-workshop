using System;
using System.CommandLine;
using TodoApp.Services;

namespace TodoApp.Cli
{
    public class AssignCommandModule : ITodoCommandModule
    {
        public Command Build(Func<ITodoService> serviceFactory)
        {
            var cmd = new Command("assign", "Assign a task to someone");

            var id = new Option<int>("--id", "Task ID") { IsRequired = true };
            var owner = new Option<string>("--owner", "Owner") { IsRequired = true };

            cmd.AddOption(id);
            cmd.AddOption(owner);

            cmd.SetHandler((int i, string o) =>
            {
                var svc = serviceFactory();
                var taskToAssign = svc.AssignOwner(i, o);
                string taskName = taskToAssign.Name ?? "";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task '{taskName}' (ID: {i}) assigned to {o}");
                Console.ResetColor();
            }, id, owner);

            return cmd;
        }
    }
}
