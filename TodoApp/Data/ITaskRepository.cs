using TodoApp.Model;

namespace TodoApp.Data
{
    public interface ITaskRepository
    {
        void LoadData();
        void Add(TodoTask task);
        TodoTask Get(int id);
        IEnumerable<TodoTask> GetAll();
        void Delete(TodoTask task);
        void Save();
    }
}
