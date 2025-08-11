using System;
using System.CommandLine;
using System.Linq;
using TodoApp.Infrastructure;
using TodoApp.Model;
using TodoApp.Services;

namespace TodoApp.Cli
{
    public class ListCommandModule : ITodoCommandModule
    {
        public Command Build(Func<ITodoService> serviceFactory)
        {
            var cmd = new Command("list", "List tasks");

            var status = new Option<string>("--status", "Filter by status");
            var owner = new Option<string>("--owner", "Filter by owner");

            cmd.AddOption(status);
            cmd.AddOption(owner);

            cmd.SetHandler((string s, string o) =>
            {
                var svc = serviceFactory();
                var taskList = svc.ListTasks(s, o).ToList();

                if (taskList.Count == 0)
                {
                    Console.WriteLine("No tasks found.");
                    return;
                }

                Console.WriteLine();
                var formatter = new ConsoleTableFormatter<TodoTask>(
                    new Dictionary<string, int>
                    {
                        ["Id"] = 4,
                        ["Name"] = 25,
                        ["Owner"] = 15,
                        ["Status"] = 12,
                        ["Description"] = 30
                    },
                    colorSelector: t =>
                        t.Status == TodoTaskStatus.Complete ? ConsoleColor.Green :
                        t.Status == TodoTaskStatus.InProgress ? ConsoleColor.Yellow :
                        ConsoleColor.White
                );
                formatter.Write(taskList);
                Console.WriteLine();
            }, status, owner);

            return cmd;
        }
    }
}
