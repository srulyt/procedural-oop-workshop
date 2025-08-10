using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TodoApp.Infrastructure
{
    public sealed class ConsoleTableFormatter<T>
    {
        private readonly Dictionary<string, int> _widths;
        private readonly Func<T, ConsoleColor?>? _colorSelector;

        public ConsoleTableFormatter(IDictionary<string, int>? columnWidths = null, Func<T, ConsoleColor?>? colorSelector = null)
        {
            _widths = columnWidths != null
                ? new Dictionary<string, int>(columnWidths, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _colorSelector = colorSelector;
        }

        public void Write(IEnumerable<T> items, TextWriter? writer = null)
        {
            writer ??= Console.Out;

            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                 .Where(p => p.GetMethod != null && p.GetMethod.IsPublic)
                                 .Select((p, idx) =>
                                 {
                                     var disp = p.GetCustomAttribute<DisplayAttribute>(inherit: true);
                                     var header = disp?.Name ?? p.Name;
                                     var orderNullable = disp?.GetOrder();
                                     int orderKey = orderNullable.HasValue ? orderNullable.Value : int.MaxValue;
                                     int width = _widths.TryGetValue(p.Name, out var w) ? w : Math.Max(1, header.Length);
                                     return new
                                     {
                                         Property = p,
                                         Header = header,
                                         OrderKey = orderKey,
                                         ReflectionIndex = idx,
                                         Width = width
                                     };
                                 })
                                 .OrderBy(x => x.OrderKey)
                                 .ThenBy(x => x.ReflectionIndex)
                                 .ToList();

            if (!props.Any())
            {
                return;
            }

            int totalWidth = props.Sum(x => x.Width) + (props.Count - 1);

            // Header
            writer.WriteLine(string.Join(" ", props.Select(x => PadOrTruncate(x.Header ?? string.Empty, x.Width))));

            // Separator
            writer.WriteLine(new string('-', totalWidth));

            // Rows
            foreach (var item in items)
            {
                ConsoleColor? previousColor = null;
                bool applied = false;

                if (_colorSelector != null && ReferenceEquals(writer, Console.Out))
                {
                    var color = _colorSelector(item);
                    if (color.HasValue)
                    {
                        previousColor = Console.ForegroundColor;
                        Console.ForegroundColor = color.Value;
                        applied = true;
                    }
                }

                try
                {
                    var cells = props.Select(x =>
                    {
                        var raw = x.Property.GetValue(item, null);
                        var s = FormatValue(raw);
                        return PadOrTruncate(s, x.Width);
                    });
                    writer.WriteLine(string.Join(" ", cells));
                }
                finally
                {
                    if (applied && previousColor.HasValue)
                    {
                        Console.ForegroundColor = previousColor.Value;
                    }
                }
            }
        }

        private static string PadOrTruncate(string value, int width)
        {
            value ??= string.Empty;
            if (width <= 0) return string.Empty;

            if (value.Length <= width)
            {
                return value.PadRight(width);
            }

            if (width >= 4)
            {
                return value.Substring(0, width - 3) + "...";
            }

            // Not enough space for ellipsis, hard cut
            return value.Substring(0, width);
        }

        private static string FormatValue(object? value)
        {
            if (value is null) return string.Empty;

            var t = value.GetType();

            // Unwrap nullable
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlying = Nullable.GetUnderlyingType(t)!;
                value = Convert.ChangeType(value, underlying);
                t = value?.GetType() ?? underlying;
            }

            if (t.IsEnum)
            {
                var name = Enum.GetName(t, value);
                if (name != null)
                {
                    var mem = t.GetMember(name).FirstOrDefault();
                    var display = mem?.GetCustomAttribute<DisplayAttribute>(inherit: false);
                    if (display?.Name != null)
                    {
                        return display.Name;
                    }
                }
                return value.ToString() ?? string.Empty;
            }

            return value.ToString() ?? string.Empty;
        }
    }
}
