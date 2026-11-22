using System;
using System.Collections.Generic;
using Elonia.Events;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
{
    private static GameSession CreateMap(GameContent content, params string[] rows)
    {
        return TownMap.Create(content, 1, rows);
    }

    private static void CheckCombat(GameContent content)
    {
        GameSession session = CreateMap(content, "@s.", ".v.");
        Actor hero = session.Player;
        Actor slime = session.Map.Actors[1];
        RuleRecorder events = new RuleRecorder(session);
        long tick = session.World.Tick;
        Actor distant = ActorFactory.Spawn(session.Map, content.Tables.Actor.Get("villager"), 2, 0,
            new Brain(new WanderBehavior()));
        Check(!session.Runner.SubmitPlayerAction(new AttackAction(distant)).IsExecuted, "두 칸 떨어진 대상에게 닿지 않는다.");
        Check(!session.Runner.SubmitPlayerAction(new AttackAction(hero)).IsExecuted, "자신은 공격할 수 없다.");
        Check(!session.Runner.SubmitPlayerAction(new TalkAction(distant)).IsExecuted
            && !session.Runner.SubmitPlayerAction(new TalkAction(hero)).IsExecuted,
            "말 걸기도 먼 대상과 자기 자신에게는 거부된다.");
        Check(AttackAction.IsInReach(hero, slime) && AttackAction.IsInReach(hero, session.Map.Actors[2]) && !AttackAction.IsInReach(hero, distant),
            "공격 거리 판정은 대각선을 포함한 여덟 이웃 칸에 닿는다.");
        Check(session.World.Tick == tick && events.Log.Count == 0, "거부된 공격은 시간과 이벤트를 소비하지 않는다.");

        slime.TakeDamage(1);
        int hp = slime.Hp;
        int attack = hero.Stat(EStat.Attack);
        int hits = (hp + attack - 1) / attack;
        for (int index = 0; index < hits; index++)
        {
            Check(slime.IsAlive, "마지막 일격 전까지는 살아 있다.");
            Check(session.Runner.SubmitPlayerAction(new AttackAction(slime)).IsExecuted, "인접한 대상을 공격한다.");
            Check(slime.Hp == Math.Max(0, hp - attack * (index + 1)), "행의 공격력만큼 피해를 주고 체력은 0에서 멈춘다.");
        }

        Check(session.World.Tick == tick + hits * ActorAction.DEFAULT_COST / hero.Speed, "공격은 기본 행동 비용을 쓴다.");
        Check(!slime.IsAlive && events.Attacks.Count >= hits, "체력이 0인 슬라임이 죽는다.");
        int heroHits = 0;
        foreach (ActorAttacked hit in events.Attacks)
        {
            if (hit.Attacker == hero)
            {
                Check(hit.Target == slime, "공격 이벤트가 대상을 전달한다.");
                heroHits++;
            }
        }

        int heroDamages = 0;
        foreach (ActorDamaged damaged in events.Damages)
        {
            if (damaged.SourceOrNull == hero)
            {
                Check(damaged.Actor == slime && damaged.Damage == attack, "공격 피해도 ActorDamaged로 오고 공격자와 공격력을 전달한다.");
                heroDamages++;
            }
        }

        Check(heroDamages == hits, "공격마다 피해 이벤트가 한 번씩 나간다.");
        Check(heroHits == hits && events.Log[^3] == $"공격 {hero.ID}>{slime.ID}" && events.Log[^2] == $"피해 {slime.ID} {attack}"
            && events.Log[^1] == $"사망 {slime.ID}", "매 공격을 한 번씩 알리고 마지막 공격은 공격, 피해, 사망 순서로 알린다.");
        Check(!session.Runner.SubmitPlayerAction(new AttackAction(slime)).IsExecuted, "죽은 적을 다시 공격할 수 없다.");
        long energy = slime.Energy;
        long actions = slime.ActionCount;
        session.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
        Check(slime.Energy == energy && slime.ActionCount == actions && session.Map.Contains(slime), "죽은 적은 맵에 남지만 에너지와 차례를 받지 않는다.");

        GameSession ambush = CreateMap(content, "@...s");
        Actor monster = ambush.Map.Actors[1];
        Brain brain = (Brain)monster.ControllerOrNull!;
        RuleRecorder ambushEvents = new RuleRecorder(ambush);
        WaitForNpc(ambush, monster);
        Check(monster.X == 3 && brain.CurrentBehaviorOrNull is ChaseBehavior chase && chase.Target == ambush.Player
            && chase.SenseRange == monster.Row().SenseRange, "감지한 슬라임이 표의 범위로 플레이어를 추격한다.");
        WaitForNpc(ambush, monster);
        WaitForNpc(ambush, monster);
        Check(monster.X == 1 && !ambush.Runner.IsGameOver, "플레이어 옆에 도착할 때까지 추격한다.");
        int requiredHits = (ambush.Player.Hp + monster.Stat(EStat.Attack) - 1) / monster.Stat(EStat.Attack);
        for (int index = 0; index < requiredHits; index++)
        {
            WaitForNpc(ambush, monster);
        }

        Check(ambush.Runner.IsGameOver && brain.CurrentBehaviorOrNull is AttackBehavior bite && bite.Target == ambush.Player,
            "닿으면 추격보다 공격을 먼저 골라 플레이어를 죽인다.");
        Check(ambushEvents.Attacks.Count == requiredHits && ambushEvents.Log[^1] == $"사망 {ambush.Player.ID}",
            "필요한 횟수만큼 때린 뒤 플레이어 사망을 알린다.");
        Check(ambushEvents.Damages.Count == requiredHits && ambushEvents.Damages[^1].SourceOrNull == monster,
            "몬스터의 공격 피해도 공격자를 담은 ActorDamaged로 온다.");
        tick = ambush.World.Tick;
        actions = monster.ActionCount;
        Check(!ambush.Runner.SubmitPlayerAction(WaitAction.DEFAULT).IsExecuted && ambush.World.Tick == tick && monster.ActionCount == actions,
            "게임 종료 뒤 입력을 거부하고 시간과 몬스터의 차례가 멈춘다.");
    }

    private static void WaitForNpc(GameSession session, Actor npc)
    {
        long before = npc.ActionCount;
        for (int index = 0; index < ActorAction.DEFAULT_COST && npc.ActionCount == before && !session.Runner.IsGameOver; index++)
        {
            session.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
        }

        if (npc.ActionCount != before + 1)
        {
            throw new InvalidOperationException($"NPC {npc.ID}의 행동 횟수가 {before}에서 {npc.ActionCount}가 되었다.");
        }
    }

    private sealed class RuleRecorder : IEventListener<ActorAttacked>, IEventListener<ActorDied>, IEventListener<StepTriggered>,
        IEventListener<ActorDamaged>, IEventListener<ItemPickedUp>, IEventListener<PropHarvested>
    {
        public List<string> Log { get; } = new List<string>();
        public List<ActorAttacked> Attacks { get; } = new List<ActorAttacked>();
        public List<ActorDamaged> Damages { get; } = new List<ActorDamaged>();
        public List<ItemPickedUp> Pickups { get; } = new List<ItemPickedUp>();
        public List<PropHarvested> Harvests { get; } = new List<PropHarvested>();

        public RuleRecorder(GameSession session)
        {
            session.World.Events.Register<ActorAttacked, ActorDied, StepTriggered, ActorDamaged, ItemPickedUp, PropHarvested>(this);
        }

        public void OnEvent(ActorAttacked message)
        {
            Attacks.Add(message);
            Log.Add($"공격 {message.Attacker.ID}>{message.Target.ID}");
        }

        public void OnEvent(ActorDied message)
        {
            Log.Add($"사망 {message.Actor.ID}");
        }

        public void OnEvent(StepTriggered message)
        {
            Log.Add($"밟기 {message.Actor.ID}>{message.Thing.ID}");
        }

        public void OnEvent(ActorDamaged message)
        {
            Damages.Add(message);
            Log.Add($"피해 {message.Actor.ID} {message.Damage}");
        }

        public void OnEvent(ItemPickedUp message)
        {
            Pickups.Add(message);
        }

        public void OnEvent(PropHarvested message)
        {
            Harvests.Add(message);
        }
    }
}
