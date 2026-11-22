using System;
using Cysharp.Text;
using Elonia.Data;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
{
    private static void CheckStats(GameContent content)
    {
        CheckStatModifiers(content);
        CheckStatLimitsAndHp(content);
        CheckRulesUseStats(content);
        CheckStatTexts(content);
    }

    private static void CheckStatTexts(GameContent content)
    {
        GameSession session = CreateMap(content, "@");
        Actor hero = session.Player;
        ActorRow row = hero.Row();
        ActorStats stats = hero.GameStats();
        Check(DebugNames.GetStatText(stats, EStat.Speed) == ZString.Concat(row.Speed) && DebugNames.GetModifiersText(stats) == "없음",
            "수정치가 없으면 값만 쓰고 수정치는 없음이다.");
        Check(DebugNames.GetStatsSummary(stats) == ZString.Concat("최대 체력 ", row.MaxHp, ", 공격력 ", row.Attack, ", 속도 ", row.Speed),
            "모든 능력치를 EStat 순서로 한 줄에 쓴다.");

        string haste = "haste";
        string curse = "curse";
        hero.SetStatModifier(haste, EStat.Speed, 50);
        hero.SetStatModifier(curse, EStat.Attack, -1);
        Check(DebugNames.GetStatText(stats, EStat.Speed) == ZString.Concat(row.Speed + 50, " (기본 ", row.Speed, ", 수정 +50)")
            && DebugNames.GetStatText(stats, EStat.Attack) == ZString.Concat(row.Attack - 1, " (기본 ", row.Attack, ", 수정 -1)"),
            "수정치가 있으면 기본값과 부호 붙은 수정치 합을 함께 쓴다.");
        Check(DebugNames.GetModifiersText(stats) == "haste: 속도 +50\ncurse: 공격력 -1", "수정치를 출처마다 한 줄로 쓴다.");
    }

    private static void CheckStatModifiers(GameContent content)
    {
        GameSession session = CreateMap(content, "@");
        Actor hero = session.Player;
        ActorRow row = hero.Row();
        ActorStats stats = hero.GameStats();
        Check(hero.Stat(EStat.MaxHp) == row.MaxHp && hero.Stat(EStat.Attack) == row.Attack && hero.Stat(EStat.Speed) == row.Speed
            && hero.MaxHp == row.MaxHp && hero.Speed == row.Speed && stats.Modifiers.Count == 0, "수정치가 없으면 능력치는 Actor 표의 값이다.");

        string sword = "sword";
        string ring = "ring";
        hero.SetStatModifier(sword, EStat.Attack, 2);
        hero.SetStatModifier(ring, EStat.Attack, 1);
        Check(hero.Stat(EStat.Attack) == row.Attack + 3 && stats.GetBase(EStat.Attack) == row.Attack && stats.GetModifierSum(EStat.Attack) == 3,
            "출처가 다른 수정치는 더해진다.");
        hero.SetStatModifier(sword, EStat.Attack, 5);
        Check(hero.Stat(EStat.Attack) == row.Attack + 6 && stats.GetModifier(sword, EStat.Attack) == 5 && stats.Modifiers.Count == 2,
            "같은 출처와 능력치로 다시 넣으면 덮어쓴다.");
        hero.SetStatModifier(sword, EStat.Speed, 50);
        Check(hero.Speed == row.Speed + 50 && stats.Modifiers.Count == 3, "한 출처가 여러 능력치를 바꿀 수 있다.");
        hero.RemoveStatModifiers(sword);
        Check(hero.Stat(EStat.Attack) == row.Attack + 1 && hero.Speed == row.Speed && stats.GetModifier(sword, EStat.Attack) == 0
            && stats.Modifiers.Count == 1, "출처를 지우면 그 출처가 준 값만 모두 빠진다.");
        hero.SetStatModifier(ring, EStat.Attack, 0);
        Check(hero.Stat(EStat.Attack) == row.Attack && stats.Modifiers.Count == 0, "0을 넣으면 수정치가 지워진다.");
        hero.RemoveStatModifiers(ring);
        Check(stats.Modifiers.Count == 0, "준 것이 없는 출처를 지워도 아무 일이 없다.");
        CheckThrows<ArgumentNullException>(() => hero.SetStatModifier(null!, EStat.Attack, 1), "출처 없는 수정치는 넣을 수 없다.");

        string[] sources = new string[] { "first", "second", "third" };
        for (int step = 0; step < 30; step++)
        {
            string source = sources[step % sources.Length];
            EStat stat = (EStat)(step % 3);
            if (step % 7 == 6)
            {
                hero.RemoveStatModifiers(source);
            }
            else
            {
                hero.SetStatModifier(source, stat, step - 10);
            }
        }

        bool bSumsMatch = true;
        foreach (EStat stat in new EStat[] { EStat.MaxHp, EStat.Attack, EStat.Speed })
        {
            int recounted = 0;
            for (int index = 0; index < stats.Modifiers.Count; index++)
            {
                if (stats.Modifiers[index].Stat == stat)
                {
                    recounted += stats.Modifiers[index].Amount;
                }
            }

            bSumsMatch &= recounted == stats.GetModifierSum(stat);
        }

        Check(bSumsMatch && stats.Modifiers.Count > 0, "미리 더해 둔 수정치 합이 목록을 다시 더한 값과 같다.");
    }

    private static void CheckStatLimitsAndHp(GameContent content)
    {
        GameSession session = CreateMap(content, "@");
        Actor hero = session.Player;
        ActorRow row = hero.Row();
        string curse = "curse";
        hero.SetStatModifier(curse, EStat.Speed, -1000);
        hero.SetStatModifier(curse, EStat.Attack, -1000);
        Check(hero.Speed == 1 && hero.Stat(EStat.Attack) == 0 && hero.GameStats().GetModifierSum(EStat.Speed) == -1000,
            "속도는 1, 공격력은 0 아래로 내려가지 않는다.");
        hero.RemoveStatModifiers(curse);

        string armor = "armor";
        hero.SetStatModifier(armor, EStat.MaxHp, 10);
        Check(hero.MaxHp == row.MaxHp + 10 && hero.Hp == row.MaxHp, "최대 체력이 늘어도 체력을 채우지 않는다.");
        hero.Heal(100);
        Check(hero.Hp == row.MaxHp + 10, "회복은 늘어난 최대 체력까지 된다.");
        hero.RemoveStatModifiers(armor);
        Check(hero.MaxHp == row.MaxHp && hero.Hp == row.MaxHp, "최대 체력이 줄면 체력도 함께 잘린다.");
        hero.SetStatModifier(armor, EStat.MaxHp, -1000);
        Check(hero.MaxHp == 1 && hero.Hp == 1 && hero.IsAlive, "최대 체력은 1 아래로 내려가지 않아 수정치만으로는 죽지 않는다.");
        hero.RemoveStatModifiers(armor);
        Check(hero.MaxHp == row.MaxHp && hero.Hp == 1, "최대 체력이 돌아와도 잘린 체력은 그대로다.");
    }

    private static void CheckRulesUseStats(GameContent content)
    {
        GameSession fight = CreateMap(content, "@s");
        Actor hero = fight.Player;
        Actor slime = fight.Map.Actors[1];
        string sword = "sword";
        hero.SetStatModifier(sword, EStat.Attack, 2);
        Check(fight.Runner.SubmitPlayerAction(new AttackAction(slime)).IsExecuted && slime.Hp == slime.MaxHp - hero.Row().Attack - 2,
            "공격 피해는 공격력 수정치까지 더한 값이다.");

        string weakness = "weakness";
        slime.SetStatModifier(weakness, EStat.Attack, -slime.Row().Attack);
        Check(!slime.IsThreat(), "공격력이 0이 된 적은 위협으로 세지 않는다.");

        GameSession run = CreateMap(content, "@");
        Actor runner = run.Player;
        string haste = "haste";
        runner.SetStatModifier(haste, EStat.Speed, runner.Row().Speed);
        Check(run.Runner.GetEnergyPerTick(runner) == runner.Row().Speed * 2, "엔진은 수정치까지 더한 속도로 에너지를 준다.");

        run.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
        long tick = run.World.Tick;
        run.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
        long tickAfterOne = run.World.Tick;
        run.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
        Check(tickAfterOne == tick && run.World.Tick == tick + 1, "속도가 두 배면 한 틱에 두 번 행동한다.");
    }
}
