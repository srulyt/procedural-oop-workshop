using System.ComponentModel.DataAnnotations;
using TodoApp.Model;

namespace TodoApp.Infrastructure
{
    public class TodoStatusParser : ITodoStatusParser
    {
        public bool TryParse(string? input, out TodoTaskStatus status)
        {
            status = TodoTaskStatus.Todo;
            if (string.IsNullOrWhiteSpace(input))
                return true;

            string target = Normalize(input);

            foreach (TodoTaskStatus s in Enum.GetValues(typeof(TodoTaskStatus)))
            {
                var display = GetDisplayName(s);
                if (Normalize(display) == target || Normalize(s.ToString()) == target)
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

        private static string Normalize(string s)
            => new string(s.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
    }
}
