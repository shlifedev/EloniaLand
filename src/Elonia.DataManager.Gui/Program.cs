using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Elonia.DataManager.Gui;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            return RunDesktop(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunDesktop(string[] args)
    {
        string? rootOrNull = FindRepositoryOrNull(AppContext.BaseDirectory) ?? FindRepositoryOrNull(Environment.CurrentDirectory);
        if (rootOrNull == null)
        {
            Console.Error.WriteLine("Elonia 저장소에서 실행하세요. Elonia.sln과 Desktop/scripts/launch.mjs를 찾을 수 없습니다.");
            return 1;
        }

        ProcessStartInfo start = new ProcessStartInfo(FindNode())
        {
            WorkingDirectory = rootOrNull,
            UseShellExecute = false
        };
        start.ArgumentList.Add(Path.Combine(rootOrNull, "src", "Elonia.DataManager.Gui", "Desktop", "scripts", "launch.mjs"));
        foreach (string argument in args)
        {
            start.ArgumentList.Add(argument);
        }

        using Process launcher = Process.Start(start) ?? throw new IOException("데이터매니저를 시작하지 못했습니다.");
        void StopLauncher()
        {
            try
            {
                if (!launcher.HasExited)
                {
                    launcher.Kill(true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }

        void CancelFromConsole(object? senderOrNull, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            StopLauncher();
        }

        void StopOnExit(object? senderOrNull, EventArgs e)
        {
            StopLauncher();
        }

        using PosixSignalRegistration? terminationOrNull = OperatingSystem.IsWindows() ? null
            : PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                context.Cancel = true;
                StopLauncher();
            });
        AppDomain.CurrentDomain.ProcessExit += StopOnExit;
        Console.CancelKeyPress += CancelFromConsole;
        try
        {
            launcher.WaitForExit();
            return launcher.ExitCode;
        }
        finally
        {
            Console.CancelKeyPress -= CancelFromConsole;
            AppDomain.CurrentDomain.ProcessExit -= StopOnExit;
        }
    }

    private static string? FindRepositoryOrNull(string start)
    {
        for (DirectoryInfo? directoryOrNull = new DirectoryInfo(start); directoryOrNull != null; directoryOrNull = directoryOrNull.Parent)
        {
            string root = directoryOrNull.FullName;
            if (File.Exists(Path.Combine(root, "Elonia.sln"))
                && File.Exists(Path.Combine(root, "src", "Elonia.DataManager.Gui", "Desktop", "scripts", "launch.mjs")))
            {
                return root;
            }
        }
        return null;
    }

    private static string FindNode()
    {
        if (OperatingSystem.IsMacOS())
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = Path.Combine(directory, "node");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            if (File.Exists("/opt/homebrew/bin/node"))
            {
                return "/opt/homebrew/bin/node";
            }
            if (File.Exists("/usr/local/bin/node"))
            {
                return "/usr/local/bin/node";
            }
        }
        return "node";
    }
}
