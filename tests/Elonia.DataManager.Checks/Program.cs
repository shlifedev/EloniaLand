using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Elonia.DataManager;

namespace Elonia.Checks;

public static partial class Program
{
    private const string UPDATE_FIXTURE_ARGUMENT = "--update-fixture";

    private static int _failureCount;
    private static int _checkCount;

    public static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == UPDATE_FIXTURE_ARGUMENT)
        {
            return UpdateFixture();
        }

        Run(CheckCellText);
        Run(CheckSheetRoles);
        Run(CheckTypeExpressions);
        Run(CheckScalarTypes);
        Run(CheckEnumExpressions);
        Run(CheckFixtureBuilds);
        Run(CheckFixtureContents);
        Run(CheckGeneratedCodeIsCurrent);
        Run(CheckBuildIsDeterministic);
        Run(CheckDiagnostics);
        Run(CheckEveryErrorIsReported);
        Run(CheckManifest);
        Run(CheckCommitter);
        Run(CheckXlsxFiles);
        Run(CheckWorkspace);
        Run(CheckGameData);
#if HAS_FIXTURE
        Run(CheckRoundTrip);
        Run(CheckLoadRejectsMismatch);
#else
        Check(false, "The fixture code is missing. Run this project with --update-fixture, then run the checks again.");
#endif

        Console.WriteLine($"Data manager checks: {_checkCount - _failureCount}/{_checkCount} passed.");
        return _failureCount == 0 ? 0 : 1;
    }

    private static int UpdateFixture()
    {
        TableBuildResult result = BuildFixture();
        if (!result.IsSuccess)
        {
            Console.Write(DiagnosticFormatter.Format(result.Diagnostics.Items));
            return 1;
        }

        GeneratedOutput codeOnly = new GeneratedOutput();
        foreach (KeyValuePair<string, string> file in result.OutputOrNull!.CodeFiles)
        {
            codeOnly.CodeFiles.Add(file.Key, file.Value);
        }

        string directory = GetFixtureCodeDirectory();
        CommitReport report = OutputCommitter.Commit(codeOnly, directory, directory);
        Console.WriteLine($"Fixture code: {report.Written.Count} written, {report.Deleted.Count} deleted, {report.UnchangedCount} unchanged in {directory}");
        return 0;
    }

    private static TableBuildResult BuildFixture()
    {
        return TableBuild.Run(new MemoryWorkbookSource(Fixture.CreateWorkbook()), Fixture.CreateOptions());
    }

    private static string GetFixtureCodeDirectory()
    {
        return Path.Combine(GetProjectDirectory(), "Fixture", "Generated");
    }

    private static string GetProjectDirectory([CallerFilePath] string sourcePath = "")
    {
        return Path.GetDirectoryName(sourcePath)!;
    }

    private static void Run(Action test)
    {
        try
        {
            test();
        }
        catch (Exception exception)
        {
            Check(false, $"{test.Method.Name} raised an unexpected exception: {exception}");
        }
    }

    private static void CheckThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Check(true, message);
            return;
        }
        catch (Exception exception)
        {
            Check(false, $"{message} Expected {typeof(TException).Name}, got {exception.GetType().Name}.");
            return;
        }

        Check(false, $"{message} No exception was thrown.");
    }

    private static void CheckEqual<T>(T expected, T actual, string message)
    {
        bool bEqual = EqualityComparer<T>.Default.Equals(expected, actual);
        Check(bEqual, $"{message} Expected {expected}, got {actual}.");
    }

    private static void Check(bool bCondition, string message)
    {
        _checkCount++;
        if (!bCondition)
        {
            _failureCount++;
            Console.Error.WriteLine($"FAIL: {message}");
        }
    }
}
