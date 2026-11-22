using System;
using Elonia.Commands.Checks;
using Elonia.Events.Checks;
using Elonia.Game.Checks;
using Elonia.Simulation.Checks;
using static Elonia.Checks.Checker;

namespace Elonia.Checks;

internal static class Program
{
    public static int Main()
    {
        RunSuite("Events", EventChecks.RunAll);
        RunSuite("Commands", CommandChecks.RunAll);
        RunSuite("Simulation", SimulationChecks.RunAll);
        RunSuite("Game", GameChecks.RunAll);

        Console.WriteLine($"All checks: {CheckCount - FailureCount}/{CheckCount} passed.");
        return FailureCount == 0 ? 0 : 1;
    }

    private static void RunSuite(string name, Action runAll)
    {
        int checkCountBefore = CheckCount;
        int failureCountBefore = FailureCount;
        Run(runAll, name);

        int checkCount = CheckCount - checkCountBefore;
        int failureCount = FailureCount - failureCountBefore;
        Console.WriteLine($"{name} checks: {checkCount - failureCount}/{checkCount} passed.");
    }
}
