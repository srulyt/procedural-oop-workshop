using TodoApp.Model;

namespace TodoApp.Infrastructure
{
    public interface ITodoStatusParser
    {
        bool TryParse(string? input, out TodoTaskStatus status);
    }
}
