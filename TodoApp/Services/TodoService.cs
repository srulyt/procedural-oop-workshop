using TodoApp.Data;
using TodoApp.Infrastructure;
using TodoApp.Model;

namespace TodoApp.Services
{
    public class TodoService : ITodoService
    {
        private readonly ITaskRepository _repo;
        private readonly ITodoStatusParser _parser;

        public TodoService(ITaskRepository repo, ITodoStatusParser parser)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));

            try
            {
                _repo.LoadData();
            }
            catch (Exception ex)
            {
                throw new PersistenceException($"Error loading tasks: {ex.Message}");
            }
        }

        public TodoTask AddTask(string name, string? owner, string? status, string? description)
        {
            // Name should be required by CLI, but validate defensively
            if (string.IsNullOrWhiteSpace(name))
                throw new TodoValidationException("Name is required.");

            if (!_parser.TryParse(status, out var statusEnum))
                throw new TodoValidationException("Error: Invalid status. Allowed values: Todo, In Progress, Complete");

            var newTask = new TodoTask
            {
                Name = name,
                Owner = owner ?? "Unassigned",
                Status = statusEnum,
                Description = description ?? string.Empty
            };

            try
            {
                _repo.Add(newTask);
                _repo.Save();
            }
            catch (Exception ex)
            {
                throw new PersistenceException($"Error saving task: {ex.Message}");
            }

            return newTask;
        }

        public IEnumerable<TodoTask> ListTasks(string? statusFilter, string? ownerFilter)
        {
            IEnumerable<TodoTask> tasks = _repo.GetAll();

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                if (_parser.TryParse(statusFilter, out var s))
                {
                    tasks = tasks.Where(t => t.Status == s);
                }
                else
                {
                    // Match existing behavior: invalid status filter yields empty result, not an error.
                    return Enumerable.Empty<TodoTask>();
                }
            }

            if (!string.IsNullOrWhiteSpace(ownerFilter))
            {
                string owner = ownerFilter!;
                tasks = tasks.Where(t => (t.Owner ?? string.Empty).Equals(owner, StringComparison.OrdinalIgnoreCase));
            }

            return tasks;
        }

        public bool UpdateTask(int id, string? name, string? owner, string? status, string? description)
        {
            var task = GetByIdOrThrow(id);

            bool updated = false;

            if (!string.IsNullOrWhiteSpace(name))
            {
                task.Name = name!;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                task.Owner = owner!;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!_parser.TryParse(status, out var newStatus))
                    throw new TodoValidationException("Error: Invalid status. Allowed values: Todo, In Progress, Complete");

                task.Status = newStatus;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                task.Description = description!;
                updated = true;
            }

            if (!updated)
                return false;

            try
            {
                _repo.Save();
            }
            catch (Exception ex)
            {
                throw new PersistenceException($"Error saving task: {ex.Message}");
            }

            return true;
        }

        public TodoTask DeleteTask(int id)
        {
            var task = GetByIdOrThrow(id);

            try
            {
                _repo.Delete(task);
                _repo.Save();
            }
            catch (Exception ex)
            {
                throw new PersistenceException($"Error saving task: {ex.Message}");
            }

            return task;
        }

        public TodoTask CompleteTask(int id)
        {
            var task = GetByIdOrThrow(id);
            task.Status = TodoTaskStatus.Complete;

            try
            {
                _repo.Save();
            }
            catch (Exception ex)
            {
                throw new PersistenceException($"Error saving task: {ex.Message}");
            }

            return task;
        }

        public TodoTask AssignOwner(int id, string owner)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new TodoValidationException("Owner is required.");

            var task = GetByIdOrThrow(id);
            task.Owner = owner;

            try
            {
                _repo.Save();
            }
            catch (Exception ex)
            {
                throw new PersistenceException($"Error saving task: {ex.Message}");
            }

            return task;
        }

        private TodoTask GetByIdOrThrow(int id)
        {
            try
            {
                return _repo.Get(id);
            }
            catch
            {
                throw new NotFoundException($"Error: Task with ID {id} not found");
            }
        }
    }
}
