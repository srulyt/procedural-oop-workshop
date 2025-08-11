using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using TodoApp.Infrastructure;
using TodoApp.Services;

namespace TodoApp.Cli
{
    // Single-responsibility module that contributes a Command
    public interface ITodoCommandModule
    {
        Command Build(Func<ITodoService> serviceFactory);
    }

    // Builder to compose modules and centralize CLI configuration
    public class TodoCliBuilder
    {
        private readonly RootCommand _root;
        private readonly List<ITodoCommandModule> _modules = new();

        public TodoCliBuilder(string? description = null)
        {
            _root = new RootCommand(description ?? "TodoApp - CLI Task Manager");
        }

        public TodoCliBuilder AddModule(ITodoCommandModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            _modules.Add(module);
            return this;
        }

        public TodoCliBuilder UseDefaultModules()
        {
            AddModule(new AddCommandModule());
            AddModule(new ListCommandModule());
            AddModule(new UpdateCommandModule());
            AddModule(new DeleteCommandModule());
            AddModule(new CompleteCommandModule());
            AddModule(new AssignCommandModule());
            return this;
        }

        public Parser BuildParser(Func<ITodoService> serviceFactory)
        {
            if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));

            foreach (var module in _modules)
            {
                _root.AddCommand(module.Build(serviceFactory));
            }

            var builder = new CommandLineBuilder(_root)
                .UseDefaults() // enables built-in help, suggestions, etc.
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
                    // Other exceptions use default behavior (preserve stack trace for unexpected failures)
                });

            return builder.Build();
        }

        public RootCommand Root => _root;
    }
}
