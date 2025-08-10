using System.ComponentModel.DataAnnotations;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using TodoApp.Model;
using TodoApp.Infrastructure;

namespace TodoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Use repository for persistence
            var repo = new TaskRepository();
            try
            {
                repo.LoadData();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error loading tasks: {ex.Message}");
                Console.ResetColor();
                return;
            }

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
                // Create new task
                if (!TryParseStatus(status, out var statusEnum))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid status. Allowed values: Todo, In Progress, Complete");
                    Console.ResetColor();
                    return;
                }
                var newTask = new TodoTask
                {
                    // Id assigned by repository
                    Name = name,
                    Owner = owner,
                    Status = statusEnum,
                    Description = description
                };

                try
                {
                    repo.Add(newTask);
                    repo.Save();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task '{name}' added successfully with ID {newTask.Id}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error saving task: {ex.Message}");
                    Console.ResetColor();
                }
            }, addName, addOwner, addStatus, addDesc);

            // list command
            var list = new Command("list", "List tasks");
            var listStatus = new Option<string>("--status", "Filter by status");
            var listOwner = new Option<string>("--owner", "Filter by owner");
            list.AddOption(listStatus);
            list.AddOption(listOwner);

            list.SetHandler((string status, string owner) =>
            {
                // Filter tasks
                var filteredTasks = repo.GetAll();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    string statusFilter = status;
                    if (TryParseStatus(statusFilter, out var statusEnumFilter))
                    {
                        filteredTasks = filteredTasks.Where(t => t.Status == statusEnumFilter);
                    }
                    else
                    {
                        filteredTasks = Enumerable.Empty<TodoTask>();
                    }
                }

                if (!string.IsNullOrEmpty(owner))
                {
                    string ownerFilter = owner;
                    filteredTasks = filteredTasks.Where(t =>
                        (t.Owner ?? string.Empty).Equals(ownerFilter, StringComparison.OrdinalIgnoreCase));
                }

                var taskList = filteredTasks.ToList();

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


                // Find task via repository
                TodoTask taskToUpdate;
                try
                {
                    taskToUpdate = repo.Get(id);
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {id} not found");
                    Console.ResetColor();
                    return;
                }

                // Update properties
                bool updated = false;
                if (!string.IsNullOrEmpty(name))
                {
                    taskToUpdate.Name = name;
                    updated = true;
                }
                if (!string.IsNullOrEmpty(owner))
                {
                    taskToUpdate.Owner = owner;
                    updated = true;
                }
                if (!string.IsNullOrEmpty(status))
                {
                    if (!TryParseStatus(status, out var statusEnumUpdate))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: Invalid status. Allowed values: Todo, In Progress, Complete");
                        Console.ResetColor();
                        return;
                    }
                    taskToUpdate.Status = statusEnumUpdate;
                    updated = true;
                }
                if (!string.IsNullOrEmpty(description))
                {
                    taskToUpdate.Description = description;
                    updated = true;
                }

                if (!updated)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Warning: No properties specified to update");
                    Console.ResetColor();
                    return;
                }

                // Save to file via repository
                try
                {
                    repo.Save();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task {id} updated successfully");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error saving task: {ex.Message}");
                    Console.ResetColor();
                }
            }, updId, updName, updOwner, updStatus, updDesc);

            // delete command
            var delete = new Command("delete", "Delete a task");
            var delId = new Option<int>("--id", "Task ID");
            delId.IsRequired = true;
            delete.AddOption(delId);

            delete.SetHandler((int id) =>
            {


                // Find and remove task
                TodoTask taskToDelete;
                try
                {
                    taskToDelete = repo.Get(id);
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {id} not found");
                    Console.ResetColor();
                    return;
                }

                string taskName = taskToDelete.Name ?? "";
                try
                {
                    repo.Delete(taskToDelete);
                    repo.Save();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task '{taskName}' (ID: {id}) deleted successfully");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error saving tasks: {ex.Message}");
                    Console.ResetColor();
                }
            }, delId);

            // complete command
            var complete = new Command("complete", "Mark a task as complete");
            var compId = new Option<int>("--id", "Task ID");
            compId.IsRequired = true;
            complete.AddOption(compId);

            complete.SetHandler((int id) =>
            {


                // Find task
                TodoTask taskToComplete;
                try
                {
                    taskToComplete = repo.Get(id);
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {id} not found");
                    Console.ResetColor();
                    return;
                }

                string taskName = taskToComplete.Name ?? "";
                taskToComplete.Status = TodoTaskStatus.Complete;

                // Save to file
                try
                {
                    repo.Save();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task '{taskName}' (ID: {id}) marked as complete");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error saving task: {ex.Message}");
                    Console.ResetColor();
                }
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


                // Find task
                TodoTask taskToAssign;
                try
                {
                    taskToAssign = repo.Get(id);
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {id} not found");
                    Console.ResetColor();
                    return;
                }

                string taskName = taskToAssign.Name ?? "";
                string newOwner = owner;
                taskToAssign.Owner = newOwner;

                // Save to file
                try
                {
                    repo.Save();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task '{taskName}' (ID: {id}) assigned to {newOwner}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error saving task: {ex.Message}");
                    Console.ResetColor();
                }
            }, asgId, asgOwner);

            // Register commands
            root.AddCommand(add);
            root.AddCommand(list);
            root.AddCommand(update);
            root.AddCommand(delete);
            root.AddCommand(complete);
            root.AddCommand(assign);

            // Build parser and preserve legacy help/unknown behavior
            var parser = new CommandLineBuilder(root).UseDefaults().Build();

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

        private static bool TryParseStatus(string input, out TodoTaskStatus status)
        {
            status = TodoTaskStatus.Todo;
            if (string.IsNullOrWhiteSpace(input)) return true;

            string Norm(string s) => new string(s.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
            var target = Norm(input);

            foreach (TodoTaskStatus s in Enum.GetValues(typeof(TodoTaskStatus)))
            {
                var display = GetDisplayName(s);
                if (Norm(display) == target || s.ToString().ToLowerInvariant() == target)
                {
                    status = s;
                    return true;
                }
            }
            return false;
        }

        private static string GetDisplayName(TodoTaskStatus status)
        {
            var mem = typeof(TodoTaskStatus).GetMember(status.ToString()).FirstOrDefault();
            var display = mem?.GetCustomAttributes(typeof(DisplayAttribute), false).OfType<DisplayAttribute>().FirstOrDefault();
            return display?.Name ?? status.ToString();
        }
    }
}
