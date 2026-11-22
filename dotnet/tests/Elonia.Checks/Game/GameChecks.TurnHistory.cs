using System;
using System.Collections.Generic;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
{
    private static void CheckTurnHistory(GameContent content)
    {
        GameSession town = CreatePlay(content, content.Tables, "hero", "villager").Session;
        TurnHistory history = new TurnHistory(town.Player);
        IReadOnlyList<TurnHistory.Entry> entries = history.Entries;
        Actor npc = town.Map.Actors[1];
        for (int tick = 1; tick <= 905; tick++)
        {
            Actor actor = tick == 902 ? town.Player : npc;
            history.OnEvent(new ActionExecuted(actor, WaitAction.DEFAULT, 1, tick));
        }

        Check(entries.Count == 300 && entries[0].Tick == 606 && entries[299].Tick == 905,
            "기록이 여러 바퀴 돌아도 최근 300개만 오래된 순서로 남는다.");
        int index = 0;
        bool bOrdered = true;
        foreach (TurnHistory.Entry entry in entries)
        {
            bOrdered &= entry.Tick == 606 + index && entry.Tick == entries[index].Tick;
            index++;
        }
        Check(bOrdered && index == entries.Count, "열거와 인덱스로 읽는 기록의 순서가 같다.");
        CheckThrows<ArgumentOutOfRangeException>(() => { _ = entries[-1]; }, "음수 기록 번호를 거부한다.");
        CheckThrows<ArgumentOutOfRangeException>(() => { _ = entries[entries.Count]; }, "끝을 넘는 기록 번호를 거부한다.");

        using IEnumerator<TurnHistory.Entry> iterator = entries.GetEnumerator();
        iterator.MoveNext();
        iterator.MoveNext();
        iterator.Reset();
        Check(iterator.MoveNext() && iterator.Current.Tick == 606, "열거자를 되감으면 가장 오래된 기록부터 다시 읽는다.");
        history.Clear();
        CheckThrows<InvalidOperationException>(() => iterator.MoveNext(), "기록을 비우기 전에 얻은 열거자는 무효가 된다.");
        Check(entries.Count == 0 && history.LastPlayerActionTick == 902 && history.NpcActionCountSincePlayerAction == 3,
            "기록을 비워도 직전 플레이어 행동 요약은 유지한다.");
        history.OnEvent(new ActionExecuted(npc, WaitAction.DEFAULT, 7, 1000));
        Check(entries.Count == 1 && entries[0].Tick == 1000 && entries[0].Actor == npc
            && entries[0].Action == WaitAction.DEFAULT && entries[0].Cost == 7, "비운 기록에 새 행동을 그대로 담는다.");
    }
}
