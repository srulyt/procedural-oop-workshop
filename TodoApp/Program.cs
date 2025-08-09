using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

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
            List<Dictionary<string, object>> tasks = new List<Dictionary<string, object>>();
            int nextId = 1;

            if (File.Exists(dataFile))
            {
                try
                {
                    string jsonContent = File.ReadAllText(dataFile);
                    if (!string.IsNullOrWhiteSpace(jsonContent))
                    {
                        var loadedTasks = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
                        if (loadedTasks != null)
                        {
                            tasks = loadedTasks;
                            if (tasks.Count > 0)
                            {
                                var maxId = tasks.Max(t => Convert.ToInt32(((JsonElement)t["Id"]).GetInt32()));
                                nextId = maxId + 1;
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
                var newTask = new Dictionary<string, object>
                {
                    ["Id"] = nextId,
                    ["Name"] = name,
                    ["Owner"] = owner,
                    ["Status"] = status,
                    ["Description"] = description
                };

                tasks.Add(newTask);
                nextId++;

                // Save to file
                try
                {
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dataFile, jsonOutput);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Task '{name}' added successfully with ID {newTask["Id"]}");
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
                    filteredTasks = filteredTasks.Where(t => 
                        ((JsonElement)t["Status"]).GetString().Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (parsedArgs.ContainsKey("owner"))
                {
                    string ownerFilter = parsedArgs["owner"];
                    filteredTasks = filteredTasks.Where(t => 
                        ((JsonElement)t["Owner"]).GetString().Equals(ownerFilter, StringComparison.OrdinalIgnoreCase));
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
                    int id = ((JsonElement)task["Id"]).GetInt32();
                    string name = ((JsonElement)task["Name"]).GetString() ?? "";
                    string owner = ((JsonElement)task["Owner"]).GetString() ?? "";
                    string status = ((JsonElement)task["Status"]).GetString() ?? "";
                    string description = ((JsonElement)task["Description"]).GetString() ?? "";

                    // Truncate long strings
                    if (name.Length > 25) name = name.Substring(0, 22) + "...";
                    if (owner.Length > 15) owner = owner.Substring(0, 12) + "...";
                    if (status.Length > 12) status = status.Substring(0, 9) + "...";
                    if (description.Length > 30) description = description.Substring(0, 27) + "...";

                    // Color code status
                    if (status.Equals("Complete", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (status.Equals("In Progress", StringComparison.OrdinalIgnoreCase))
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
                var taskToUpdate = tasks.FirstOrDefault(t => ((JsonElement)t["Id"]).GetInt32() == taskId);
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
                    taskToUpdate["Name"] = parsedArgs["name"];
                    updated = true;
                }
                if (parsedArgs.ContainsKey("owner"))
                {
                    taskToUpdate["Owner"] = parsedArgs["owner"];
                    updated = true;
                }
                if (parsedArgs.ContainsKey("status"))
                {
                    taskToUpdate["Status"] = parsedArgs["status"];
                    updated = true;
                }
                if (parsedArgs.ContainsKey("description"))
                {
                    taskToUpdate["Description"] = parsedArgs["description"];
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
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
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
                var taskToDelete = tasks.FirstOrDefault(t => ((JsonElement)t["Id"]).GetInt32() == taskId);
                if (taskToDelete == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {taskId} not found");
                    Console.ResetColor();
                    return;
                }

                string taskName = ((JsonElement)taskToDelete["Name"]).GetString() ?? "";
                tasks.Remove(taskToDelete);

                // Save to file
                try
                {
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
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
                var taskToComplete = tasks.FirstOrDefault(t => ((JsonElement)t["Id"]).GetInt32() == taskId);
                if (taskToComplete == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {taskId} not found");
                    Console.ResetColor();
                    return;
                }

                string taskName = ((JsonElement)taskToComplete["Name"]).GetString() ?? "";
                taskToComplete["Status"] = "Complete";

                // Save to file
                try
                {
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
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
                var taskToAssign = tasks.FirstOrDefault(t => ((JsonElement)t["Id"]).GetInt32() == taskId);
                if (taskToAssign == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Task with ID {taskId} not found");
                    Console.ResetColor();
                    return;
                }

                string taskName = ((JsonElement)taskToAssign["Name"]).GetString() ?? "";
                string newOwner = parsedArgs["owner"];
                taskToAssign["Owner"] = newOwner;

                // Save to file
                try
                {
                    string jsonOutput = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
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
    }
}
