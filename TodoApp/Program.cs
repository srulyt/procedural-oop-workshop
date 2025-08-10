using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using TodoApp.Model;
using TodoApp.Infrastructure;
using TodoApp.Services;
using TodoApp.Data;

namespace TodoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Service factory so data loads and errors surface within command execution
            Func<ITodoService> svcFactory = () => new TodoService(new TaskRepository(), new TodoStatusParser());

            // System.CommandLine v2 beta5 API: RootCommand/Command/Option + CommandHandler
            var root = new RootCommand("TodoApp - CLI Task Manager");

            // add command
            var add = new Command("add", "Add a new task");
            var addName = new Option<string>("--name", "Task name");
            addName.IsRequired = true;
            var addOwner = new Option<string>("--owner", "Task owner");
            addOwner.SetDefaultValue("Unassigned");
            var addStatus = new Option<string>("--status", "Task status");
            addStatus.SetDefaultValue("Todo");
            var addDesc = new Option<string>("--description", "Task description");
            addDesc.SetDefaultValue(string.Empty);
            add.AddOption(addName);
            add.AddOption(addOwner);
            add.AddOption(addStatus);
            add.AddOption(addDesc);

            add.SetHandler((string name, string owner, string status, string description) =>
            {
                var svc = svcFactory();
                var newTask = svc.AddTask(name, owner, status, description);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task '{newTask.Name}' added successfully with ID {newTask.Id}");
                Console.ResetColor();
            }, addName, addOwner, addStatus, addDesc);

            // list command
            var list = new Command("list", "List tasks");
            var listStatus = new Option<string>("--status", "Filter by status");
            var listOwner = new Option<string>("--owner", "Filter by owner");
            list.AddOption(listStatus);
            list.AddOption(listOwner);

            list.SetHandler((string status, string owner) =>
            {
                var svc = svcFactory();
                var taskList = svc.ListTasks(status, owner).ToList();

                if (taskList.Count == 0)
                {
                    Console.WriteLine("No tasks found.");
                    return;
                }

                // Display tasks in table format
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
            }, listStatus, listOwner);

            // update command
            var update = new Command("update", "Update an existing task");
            var updId = new Option<int>("--id", "Task ID");
            updId.IsRequired = true;
            var updName = new Option<string>("--name", "New name");
            var updOwner = new Option<string>("--owner", "New owner");
            var updStatus = new Option<string>("--status", "New status");
            var updDesc = new Option<string>("--description", "New description");
            update.AddOption(updId);
            update.AddOption(updName);
            update.AddOption(updOwner);
            update.AddOption(updStatus);
            update.AddOption(updDesc);

            update.SetHandler((int id, string name, string owner, string status, string description) =>
            {
                var svc = svcFactory();
                var changed = svc.UpdateTask(id, name, owner, status, description);
                if (!changed)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Warning: No properties specified to update");
                    Console.ResetColor();
                    return;
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task {id} updated successfully");
                Console.ResetColor();
            }, updId, updName, updOwner, updStatus, updDesc);

            // delete command
            var delete = new Command("delete", "Delete a task");
            var delId = new Option<int>("--id", "Task ID");
            delId.IsRequired = true;
            delete.AddOption(delId);

            delete.SetHandler((int id) =>
            {
                var svc = svcFactory();
                var taskToDelete = svc.DeleteTask(id);
                string taskName = taskToDelete.Name ?? "";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task '{taskName}' (ID: {id}) deleted successfully");
                Console.ResetColor();
            }, delId);

            // complete command
            var complete = new Command("complete", "Mark a task as complete");
            var compId = new Option<int>("--id", "Task ID");
            compId.IsRequired = true;
            complete.AddOption(compId);

            complete.SetHandler((int id) =>
            {
                var svc = svcFactory();
                var taskToComplete = svc.CompleteTask(id);
                string taskName = taskToComplete.Name ?? "";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task '{taskName}' (ID: {id}) marked as complete");
                Console.ResetColor();
            }, compId);

            // assign command
            var assign = new Command("assign", "Assign a task to someone");
            var asgId = new Option<int>("--id", "Task ID");
            asgId.IsRequired = true;
            var asgOwner = new Option<string>("--owner", "Owner");
            asgOwner.IsRequired = true;
            assign.AddOption(asgId);
            assign.AddOption(asgOwner);

            assign.SetHandler((int id, string owner) =>
            {
                var svc = svcFactory();
                var taskToAssign = svc.AssignOwner(id, owner);
                string taskName = taskToAssign.Name ?? "";
                string newOwner = owner;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task '{taskName}' (ID: {id}) assigned to {newOwner}");
                Console.ResetColor();
            }, asgId, asgOwner);

            // Register commands
            root.AddCommand(add);
            root.AddCommand(list);
            root.AddCommand(update);
            root.AddCommand(delete);
            root.AddCommand(complete);
            root.AddCommand(assign);

            // Build parser with centralized exception handling for service errors
            var parser = new CommandLineBuilder(root)
                .UseDefaults()
                .UseExceptionHandler((ex, context) =>
                {
                    if (ex is TodoValidationException || ex is NotFoundException || ex is PersistenceException)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.Message);
                        Console.ResetColor();
                        context.ExitCode = 1;
                        return;
                    }
                    // Let System.CommandLine handle other exceptions as usual
                })
                .Build();

            if (args.Length == 0)
            {
                Console.WriteLine("TodoApp - CLI Task Manager");
                parser.Invoke("--help");
                return;
            }

            // Explicit help handling for tests
            if (args.Length == 1 && (string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("TodoApp - CLI Task Manager");
                parser.Invoke("--help");
                return;
            }

            var first = args[0];
            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "add","list","update","delete","complete","assign","--help","-h"
            };

            if (!known.Contains(first))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Unknown command '{first}'");
                Console.WriteLine("Use 'todoapp --help' to see available commands");
                Console.ResetColor();
                return;
            }

            // Delegate to System.CommandLine
            parser.Invoke(args);
        }
    }
}
