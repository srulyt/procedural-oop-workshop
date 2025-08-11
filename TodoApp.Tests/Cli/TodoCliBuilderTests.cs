using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TodoApp.Cli;
using TodoApp.Infrastructure;
using TodoApp.Services;

namespace TodoApp.Tests.Cli
{
    [TestClass]
    public class TodoCliBuilderTests
    {
        private static (string stdout, string stderr, int exitCode) Invoke(Parser parser, params string[] args)
        {
            var prevOut = Console.Out;
            var prevErr = Console.Error;

            using var outWriter = new StringWriter();
            using var errWriter = new StringWriter();

            try
            {
                Console.SetOut(outWriter);
                Console.SetError(errWriter);

                int code = parser.Invoke(args);

                outWriter.Flush();
                errWriter.Flush();

                return (outWriter.ToString(), errWriter.ToString(), code);
            }
            finally
            {
                Console.SetOut(prevOut);
                Console.SetError(prevErr);
            }
        }

        [TestMethod]
        public void Builder_HelpFlags_PrintsBuiltInHelp()
        {
            var builder = new TodoCliBuilder().UseDefaultModules();
            var parser = builder.BuildParser(() => throw new InvalidOperationException("Service not required for help"));

            var (stdout, _, _) = Invoke(parser, "--help");

            StringAssert.Contains(stdout, "TodoApp - CLI Task Manager");
            StringAssert.Contains(stdout, "Usage:");
            StringAssert.Contains(stdout, "Commands:");
        }

        private class PingModule : ITodoCommandModule
        {
            public Command Build(Func<ITodoService> _)
            {
                var cmd = new Command("ping", "Test ping");
                cmd.SetHandler(() => Console.WriteLine("pong"));
                return cmd;
            }
        }

        [TestMethod]
        public void Builder_AddCustomModule_RegistersAndExecutes()
        {
            var builder = new TodoCliBuilder()
                .AddModule(new PingModule());
            var parser = builder.BuildParser(() => throw new InvalidOperationException("Service not required for ping"));

            var (stdout, _, code) = Invoke(parser, "ping");

            Assert.AreEqual(0, code);
            StringAssert.Contains(stdout, "pong");
        }

        private class ThrowModule : ITodoCommandModule
        {
            public Command Build(Func<ITodoService> _)
            {
                var cmd = new Command("boom", "Throw domain exception");
                cmd.SetHandler(() => throw new NotFoundException("Error: Test not found"));
                return cmd;
            }
        }

        [TestMethod]
        public void Builder_Exceptions_AreHandledByMiddleware()
        {
            var builder = new TodoCliBuilder()
                .AddModule(new ThrowModule());
            var parser = builder.BuildParser(() => throw new InvalidOperationException("Service not required for boom"));

            var (stdout, _, code) = Invoke(parser, "boom");

            Assert.AreEqual(1, code);
            StringAssert.Contains(stdout, "Error: Test not found");
        }
    }
}
