using System.CommandLine.Parsing;
using TodoApp.Cli;
using TodoApp.Data;
using TodoApp.Infrastructure;
using TodoApp.Services;

namespace TodoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Composition root: build services and CLI with modules
            Func<ITodoService> svcFactory = () => new TodoService(new TaskRepository(), new TodoStatusParser());

            var builder = new TodoCliBuilder().UseDefaultModules();
            var parser = builder.BuildParser(svcFactory);

            parser.Invoke(args);
        }
    }
}
