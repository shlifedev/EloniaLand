using System;
using Elonia.Data;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
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

    private static void MeasureCommandAllocations(GameContent content, GameTables tables)
    {
        const int WARMUP_COUNT = PlaySession.COMMAND_CAPACITY / 4;
        const int COMMAND_COUNT = PlaySession.COMMAND_CAPACITY - WARMUP_COUNT;

        PlaySession play = CreatePlay(content, tables, "hero", "grass", "grass");
        for (int index = 0; index < WARMUP_COUNT; index++)
        {
            ExecuteAlternatingCommand(play, index);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < COMMAND_COUNT; index++)
        {
            ExecuteAlternatingCommand(play, WARMUP_COUNT + index);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Check(play.CommandCount == WARMUP_COUNT + COMMAND_COUNT, "실행한 명령이 모두 기록된다.");
        Check(allocatedBytes == 0, $"데운 뒤 플레이어 명령의 실행과 기록은 할당하지 않는다. 할당: {allocatedBytes} B");
        Console.WriteLine($"플레이어 명령 할당: {allocatedBytes} B / {COMMAND_COUNT}회");
    }

    private static void ExecuteAlternatingCommand(PlaySession play, int index)
    {
        switch (index % 3)
        {
            case 0:
                play.Execute(new MoveCommand(EDirection.Right));
                break;
            case 1:
                play.Execute(new WaitCommand());
                break;
            default:
                play.Execute(new MoveCommand(EDirection.Left));
                break;
        }
    }
}
