using System;
using System.CommandLine;
using TodoApp.Model;
using TodoApp.Services;

namespace TodoApp.Cli
{
    public class AddCommandModule : ITodoCommandModule
    {
        public Command Build(Func<ITodoService> serviceFactory)
        {
            var cmd = new Command("add", "Add a new task");

            var name = new Option<string>("--name", "Task name") { IsRequired = true };
            var owner = new Option<string>("--owner", () => "Unassigned", "Task owner");
            var status = new Option<string>("--status", () => "Todo", "Task status");
            var desc = new Option<string>("--description", () => string.Empty, "Task description");

            cmd.AddOption(name);
            cmd.AddOption(owner);
            cmd.AddOption(status);
            cmd.AddOption(desc);

            cmd.SetHandler((string n, string o, string s, string d) =>
            {
                var svc = serviceFactory();
                var newTask = svc.AddTask(n, o, s, d);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task '{newTask.Name}' added successfully with ID {newTask.Id}");
                Console.ResetColor();
            }, name, owner, status, desc);

            return cmd;
        }
    }
}
