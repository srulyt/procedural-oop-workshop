using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TodoApp.Tests.Helpers
{
    public static class ConsoleTestHost
    {
        private static readonly object _lock = new();

        public static (string stdout, string stderr) RunMain(params string[] args)
        {
            lock (_lock)
            {
                var prevOut = Console.Out;
                var prevErr = Console.Error;
                var prevIn = Console.In;

                using var outWriter = new StringWriter();
                using var errWriter = new StringWriter();
                using var inReader = new StringReader(string.Empty);

                try
                {
                    Console.SetOut(outWriter);
                    Console.SetError(errWriter);
                    Console.SetIn(inReader);

                    var asm = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .FirstOrDefault(a => string.Equals(a.GetName().Name, "TodoApp", StringComparison.OrdinalIgnoreCase));
                    if (asm is null)
                    {
                        var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                        var candidate = Path.Combine(baseDir, "TodoApp.dll");
                        if (File.Exists(candidate))
                        {
                            asm = Assembly.LoadFrom(candidate);
                        }
                    }
                    if (asm is null)
                    {
                        throw new InvalidOperationException("Could not load TodoApp assembly from test output folder or AppDomain.");
                    }
                    var programType = asm.GetType("TodoApp.Program", throwOnError: true)!;
                    var main = programType.GetMethod(
                        "Main",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null,
                        types: new[] { typeof(string[]) },
                        modifiers: null
                    );

                    main!.Invoke(null, new object?[] { args });

                    outWriter.Flush();
                    errWriter.Flush();

                    return (outWriter.ToString(), errWriter.ToString());
                }
                finally
                {
                    Console.SetOut(prevOut);
                    Console.SetError(prevErr);
                    Console.SetIn(prevIn);
                }
            }
        }
    }

    [TestClass]
    public class DataSandbox
    {
        public static string AppDataDir { get; private set; } = null!;
        public static string TasksFile => Path.Combine(AppDataDir, "tasks.json");

        private static string? _backupDir;

        [AssemblyInitialize]
        public static void Init(TestContext _)
        {
            AppDataDir = GetAppDataDir();

            // Backup any existing user data directory to avoid data loss
            if (Directory.Exists(AppDataDir))
            {
                _backupDir = AppDataDir + ".backup_" + Guid.NewGuid().ToString("N");
                Directory.Move(AppDataDir, _backupDir);
            }

            Directory.CreateDirectory(AppDataDir);
            ClearTasks();
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            try
            {
                if (Directory.Exists(AppDataDir))
                {
                    Directory.Delete(AppDataDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            // Restore original user data if backed up
            if (!string.IsNullOrEmpty(_backupDir) && Directory.Exists(_backupDir))
            {
                try
                {
                    Directory.Move(_backupDir!, AppDataDir);
                }
                catch
                {
                    // If restore fails, leave backup in place
                }
            }
        }

        public static void ClearTasks()
        {
            if (File.Exists(TasksFile))
            {
                File.Delete(TasksFile);
            }
        }

        public static void SeedJson(string json)
        {
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(TasksFile, json);
        }

        private static string GetAppDataDir()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(baseDir, "TodoApp");
            }
            else
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, ".todoapp");
            }
        }
    }
}
