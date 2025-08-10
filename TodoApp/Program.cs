using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TodoApp.Model;

namespace TodoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Application data directory setup
            string appDataDir;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TodoApp");
            }
            else
            {
                appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".todoapp");
            }

            string dataFile = Path.Combine(appDataDir, "tasks.json");

            // Ensure data directory exists
            try
            {
                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error creating data directory: {ex.Message}");
                Console.ResetColor();
                return;
            }

            // Load existing tasks from JSON file
            List<TodoTask> tasks = new List<TodoTask>();
            int nextId = 1;

            if (File.Exists(dataFile))
            {
                try
                {
                    string jsonContent = File.ReadAllText(dataFile);
                    if (!string.IsNullOrWhiteSpace(jsonContent))
                    {
                        var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
                        var loadedTasks = JsonSerializer.Deserialize<List<TodoTask>>(jsonContent, options);
                        if (loadedTasks != null)
                        {
                            tasks = loadedTasks;
                            if (tasks.Count > 0)
                            {
                                nextId = tasks.Max(t => t.Id) + 1;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error loading tasks: {ex.Message}");
                    Console.ResetColor();
                    return;
                }
            }

            // Parse command line arguments
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                // Display help
                Console.WriteLine("TodoApp - CLI Task Manager");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  todoapp <command> [options]");
                Console.WriteLine();
                Console.WriteLine("Commands:");
                Console.WriteLine("  add        Add a new task");
                Console.WriteLine("  list       List tasks");
                Console.WriteLine("  update     Update an existing task");
                Console.WriteLine("  delete     Delete a task");
                Console.WriteLine("  complete   Mark a task as complete");
                Console.WriteLine("  assign     Assign a task to someone");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  todoapp add --name \"Fix bug\" --owner \"john\" --description \"Fix the login bug\"");
                Console.WriteLine("  todoapp list");
                Console.WriteLine("  todoapp list --status \"Todo\"");
                Console.WriteLine("  todoapp list --owner \"john\"");
                Console.WriteLine("  todoapp update --id 1 --name \"Fix critical bug\"");
                Console.WriteLine("  todoapp delete --id 1");
                Console.WriteLine("  todoapp complete --id 1");
                Console.WriteLine("  todoapp assign --id 1 --owner \"jane\"");
                return;
            }

            string command = args[0].ToLower();

            // Parse arguments into dictionary
            Dictionary<string, string> parsedArgs = new Dictionary<string, string>();
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    string key = args[i].Substring(2);
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        parsedArgs[key] = args[i + 1];
                        i++;
                    }
                    else
                    {
                        parsedArgs[key] = "true";
                    }
                }
            }

            // Handle commands
            if (command == "add")
            {
                // Validate required parameters
                if (!parsedArgs.ContainsKey("name"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: --name parameter is required");
                    Console.ResetColor();
                    return;
                }

                string name = parsedArgs["name"];
                string owner = parsedArgs.ContainsKey("owner") ? parsedArgs["owner"] : "Unassigned";
                string status = parsedArgs.ContainsKey("status") ? parsedArgs["status"] : "Todo";
                string description = parsedArgs.ContainsKey("description") ? parsedArgs["description"] : "";

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
                    Id = nextId,
                    Name = name,
                    Owner = owner,
                    Status = statusEnum,
                    Description = description
                };

                tasks.Add(newTask);
                nextId++;

                // Save to file
                try
                {
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
                    File.WriteAllText(dataFile, jsonOutput);
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
            }
            else if (command == "list")
            {
                // Filter tasks
                var filteredTasks = tasks.AsEnumerable();

                if (parsedArgs.ContainsKey("status"))
                {
                    string statusFilter = parsedArgs["status"];
                    if (TryParseStatus(statusFilter, out var statusEnumFilter))
                    {
                        filteredTasks = filteredTasks.Where(t => t.Status == statusEnumFilter);
                    }
                    else
                    {
                        filteredTasks = Enumerable.Empty<TodoTask>();
                    }
                }

                if (parsedArgs.ContainsKey("owner"))
                {
                    string ownerFilter = parsedArgs["owner"];
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
                Console.WriteLine($"{"ID",-4} {"Name",-25} {"Owner",-15} {"Status",-12} {"Description",-30}");
                Console.WriteLine(new string('-', 90));

                foreach (var task in taskList)
                {
                    int id = task.Id;
                    string name = task.Name ?? "";
                    string owner = task.Owner ?? "";
                    string status = GetDisplayName(task.Status);
                    string description = task.Description ?? "";

                    // Truncate long strings
                    if (name.Length > 25) name = name.Substring(0, 22) + "...";
                    if (owner.Length > 15) owner = owner.Substring(0, 12) + "...";
                    if (status.Length > 12) status = status.Substring(0, 9) + "...";
                    if (description.Length > 30) description = description.Substring(0, 27) + "...";

                    // Color code status
                    if (task.Status == TodoTaskStatus.Complete)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (task.Status == TodoTaskStatus.InProgress)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    Console.WriteLine($"{id,-4} {name,-25} {owner,-15} {status,-12} {description,-30}");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
            else if (command == "update")
            {
                if (!parsedArgs.ContainsKey("id"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: --id parameter is required");
                    Console.ResetColor();
                    return;
                }

                if (!int.TryParse(parsedArgs["id"], out int taskId))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid task ID");
                    Console.ResetColor();
                    return;
                }

                // Find task
                var taskToUpdate = tasks.FirstOrDefault(t => t.Id == taskId);
                if (taskToUpdate == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {taskId} not found");
                    Console.ResetColor();
                    return;
                }

                // Update properties
                bool updated = false;
                if (parsedArgs.ContainsKey("name"))
                {
                    taskToUpdate.Name = parsedArgs["name"];
                    updated = true;
                }
                if (parsedArgs.ContainsKey("owner"))
                {
                    taskToUpdate.Owner = parsedArgs["owner"];
                    updated = true;
                }
                if (parsedArgs.ContainsKey("status"))
                {
                    if (!TryParseStatus(parsedArgs["status"], out var statusEnumUpdate))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: Invalid status. Allowed values: Todo, In Progress, Complete");
                        Console.ResetColor();
                        return;
                    }
                    taskToUpdate.Status = statusEnumUpdate;
                    updated = true;
                }
                if (parsedArgs.ContainsKey("description"))
                {
                    taskToUpdate.Description = parsedArgs["description"];
                    updated = true;
                }

                if (!updated)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Warning: No properties specified to update");
                    Console.ResetColor();
                    return;
                }

                // Save to file
                try
                {
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
                    File.WriteAllText(dataFile, jsonOutput);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task {taskId} updated successfully");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error saving task: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else if (command == "delete")
            {
                if (!parsedArgs.ContainsKey("id"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: --id parameter is required");
                    Console.ResetColor();
                    return;
                }

                if (!int.TryParse(parsedArgs["id"], out int taskId))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid task ID");
                    Console.ResetColor();
                    return;
                }

                // Find and remove task
                var taskToDelete = tasks.FirstOrDefault(t => t.Id == taskId);
                if (taskToDelete == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {taskId} not found");
                    Console.ResetColor();
                    return;
                }

                string taskName = taskToDelete.Name ?? "";
                tasks.Remove(taskToDelete);

                // Save to file
                try
                {
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
                    File.WriteAllText(dataFile, jsonOutput);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task '{taskName}' (ID: {taskId}) deleted successfully");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error saving tasks: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else if (command == "complete")
            {
                if (!parsedArgs.ContainsKey("id"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: --id parameter is required");
                    Console.ResetColor();
                    return;
                }

                if (!int.TryParse(parsedArgs["id"], out int taskId))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid task ID");
                    Console.ResetColor();
                    return;
                }

                // Find task
                var taskToComplete = tasks.FirstOrDefault(t => t.Id == taskId);
                if (taskToComplete == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {taskId} not found");
                    Console.ResetColor();
                    return;
                }

                string taskName = taskToComplete.Name ?? "";
                taskToComplete.Status = TodoTaskStatus.Complete;

                // Save to file
                try
                {
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
                    File.WriteAllText(dataFile, jsonOutput);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task '{taskName}' (ID: {taskId}) marked as complete");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error saving task: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else if (command == "assign")
            {
                if (!parsedArgs.ContainsKey("id") || !parsedArgs.ContainsKey("owner"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: --id and --owner parameters are required");
                    Console.ResetColor();
                    return;
                }

                if (!int.TryParse(parsedArgs["id"], out int taskId))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid task ID");
                    Console.ResetColor();
                    return;
                }

                // Find task
                var taskToAssign = tasks.FirstOrDefault(t => t.Id == taskId);
                if (taskToAssign == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {taskId} not found");
                    Console.ResetColor();
                    return;
                }

                string taskName = taskToAssign.Name ?? "";
                string newOwner = parsedArgs["owner"];
                taskToAssign.Owner = newOwner;

                // Save to file
                try
                {
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
                    File.WriteAllText(dataFile, jsonOutput);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task '{taskName}' (ID: {taskId}) assigned to {newOwner}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error saving task: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Unknown command '{command}'");
                Console.WriteLine("Use 'todoapp --help' to see available commands");
                Console.ResetColor();
            }
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
