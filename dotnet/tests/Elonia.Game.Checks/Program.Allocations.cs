using System;
using Elonia.Data;
using Elonia.Game;
using Elonia.Simulation;

namespace Elonia.Checks;

internal static partial class Program
{
    private static void MeasureRuleActionAllocations(GameContent content, GameTables tables)
    {
        const int WARMUP_COUNT = 10000;
        const int ACTION_COUNT = 100000;

        PlaySession play = CreatePlay(content, tables, "hero", "grass", "grass", "grass", "grass", "villager");
        GameRunner runner = play.Session.Runner;
        ActorAction[] actions = new ActorAction[] { MoveAction.RIGHT, WaitAction.DEFAULT, MoveAction.LEFT };
        for (int index = 0; index < WARMUP_COUNT; index++)
        {
            runner.SubmitPlayerAction(actions[index % actions.Length]);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < ACTION_COUNT; index++)
        {
            runner.SubmitPlayerAction(actions[(WARMUP_COUNT + index) % actions.Length]);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Check(allocatedBytes == 0, $"데운 뒤 규칙 행동과 NPC 차례는 할당하지 않는다. 할당: {allocatedBytes} B");
        Console.WriteLine($"규칙 행동 할당: {allocatedBytes} B / {ACTION_COUNT}회");
    }
}
