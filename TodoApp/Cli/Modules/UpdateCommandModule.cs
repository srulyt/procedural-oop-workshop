using System;
using System.CommandLine;
using TodoApp.Services;

namespace TodoApp.Cli
{
    public class UpdateCommandModule : ITodoCommandModule
    {
        public Command Build(Func<ITodoService> serviceFactory)
        {
            var cmd = new Command("update", "Update an existing task");

            var id = new Option<int>("--id", "Task ID") { IsRequired = true };
            var name = new Option<string>("--name", "New name");
            var owner = new Option<string>("--owner", "New owner");
            var status = new Option<string>("--status", "New status");
            var desc = new Option<string>("--description", "New description");

            cmd.AddOption(id);
            cmd.AddOption(name);
            cmd.AddOption(owner);
            cmd.AddOption(status);
            cmd.AddOption(desc);

            cmd.SetHandler((int i, string n, string o, string s, string d) =>
            {
                var svc = serviceFactory();
                var changed = svc.UpdateTask(i, n, o, s, d);
                if (!changed)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Warning: No properties specified to update");
                    Console.ResetColor();
                    return;
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task {i} updated successfully");
                Console.ResetColor();
            }, id, name, owner, status, desc);

            return cmd;
        }
    }
}
