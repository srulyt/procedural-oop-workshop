using System.Text.Json;
using System.Text.Json.Serialization;
using TodoApp.Model;

public class TaskRepository
{
    private int _nextId = 1;
    private List<TodoTask> _tasks = new List<TodoTask>();
    private string _appDataDirectory;
    private string _dataFile;

    public TaskRepository()
    {
        // Application data directory setup
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            _appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TodoApp");
        }
        else
        {
            _appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".todoapp");
        }

        _dataFile = Path.Combine(_appDataDirectory, "tasks.json");
    }

    public void LoadData()
    {
        
        // Ensure data directory exists
        if (!Directory.Exists(_appDataDirectory))
        {
            Directory.CreateDirectory(_appDataDirectory);
        }

        // Load existing tasks from JSON file
        if (File.Exists(_dataFile))
        {
            string jsonContent = File.ReadAllText(_dataFile);
            if (!string.IsNullOrWhiteSpace(jsonContent))
            {
                var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
                var loadedTasks = JsonSerializer.Deserialize<List<TodoTask>>(jsonContent, options);
                if (loadedTasks != null)
                {
                    _tasks = loadedTasks;
                    if (_tasks.Count > 0)
                    {
                        _nextId = _tasks.Max(t => t.Id) + 1;
                    }
                }
            }

        }
    }

    public void Add(TodoTask task)
    {
        if (task.Id != default)
        {
            throw new InvalidOperationException("New tasks should not be assigned ids");
        }

        task.Id = _nextId;
        _nextId++;
        _tasks.Add(task);
    }

    public TodoTask Get(int id)
    {
        return _tasks.Single(t => t.Id == id);
    }

    public IEnumerable<TodoTask> GetAll()
    {
        return _tasks;
    }

    public void Delete(TodoTask task)
    {
        if (!_tasks.Contains(task))
        {
            throw new InvalidOperationException("Could not delete task that does not exist");
        }

        _tasks.Remove(task);
    }

    public void Save()
    {
        string jsonOutput = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
        File.WriteAllText(_dataFile, jsonOutput);
    }

}