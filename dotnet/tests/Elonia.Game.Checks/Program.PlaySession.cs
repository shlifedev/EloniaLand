using System;
using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Data;
using Elonia.Events;
using Elonia.Game;
using Elonia.Simulation;

namespace Elonia.Checks;

internal static partial class Program
{
    private static void CheckPlaySession(GameContent content, GameTables tables)
    {
        CheckBumpAttack(content, tables);
        CheckBumpHarvest(content, tables);
        CheckBlockedMove(content, tables);
        CheckMenuChoices(content, tables);
        CheckClickTravel(content, tables);
        CheckMenuTravel(content, tables);
        CheckAlerts(content, tables);
        CheckTownIntro(content);
        CheckSameSeedSameGame(content);
    }

    private static void CheckBumpAttack(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "slime");
        EventRecorder recorder = new EventRecorder(play.Session);
        Actor slime = play.Session.Map.Actors[1];
        Check(play.Execute(EPlayerCommand.MoveRight).IsExecuted, "적이 있는 쪽으로 움직이면 행동이 실행된다.");
        Check(play.Session.Player.X == 0 && slime.Hp < slime.Type.MaxHp, "적이 있는 쪽으로 움직이면 이동 대신 공격한다.");
        Check(recorder.HasMessageContaining("슬라임에게"), "공격 문장이 로그 이벤트로 나간다.");
        Check(play.GetCommandText() == "d", "부딪히기 공격도 이동 명령으로 기록된다.");
    }

    private static void CheckBumpHarvest(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "tree");
        EventRecorder recorder = new EventRecorder(play.Session);
        Check(play.Execute(EPlayerCommand.MoveRight).IsExecuted, "나무 쪽으로 움직이면 행동이 실행된다.");
        Check(play.Session.Map.GetPropOrNull(1, 0) == null && play.Session.Player.X == 0, "나무 쪽으로 움직이면 이동 대신 벤다.");
        Check(recorder.HasMessageContaining("나무를 베었다"), "채집 문장이 로그 이벤트로 나간다.");
    }

    private static void CheckBlockedMove(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "grass");
        EventRecorder recorder = new EventRecorder(play.Session);
        long tick = play.Session.World.Tick;
        Check(!play.Execute(EPlayerCommand.MoveLeft).IsExecuted, "맵 밖으로는 움직일 수 없다.");
        Check(recorder.BlockedCount == 1 && play.Session.World.Tick == tick, "막힌 쪽으로 움직이면 막힌 이동만 알리고 시간이 흐르지 않는다.");
        Check(!play.Execute(EPlayerCommand.PickUp).IsExecuted && recorder.HasMessageContaining("여기에는 주울 것이 없다"), "주울 것이 없으면 알린다.");
        Check(!play.Execute(EPlayerCommand.Restart).IsExecuted, "재시작은 호스트가 처리하므로 한 판 안에서는 거부된다.");
    }

    private static void CheckMenuChoices(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "grass", "potion");
        List<MenuChoice> choices = new List<MenuChoice>();
        play.CollectMenuChoices(2, 0, choices);
        Check(choices.Count == 2 && choices[0].Label == "물약 마시기" && choices[0].InteractionOrNull != null, "아이템 칸의 첫 항목은 그 아이템을 쓰는 일이다.");
        Check(choices[1].Label == "여기로 걸어가기" && choices[1].InteractionOrNull == null, "다른 칸에는 걸어가기 항목이 붙는다.");
        play.CollectMenuChoices(0, 0, choices);
        Check(choices.Count == 0, "아무것도 없는 플레이어 칸에는 고를 일이 없다.");
        play.CollectMenuChoices(9, 0, choices);
        Check(choices.Count == 0, "맵 밖에는 고를 일이 없다.");
    }

    private static void CheckClickTravel(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "grass", "grass", "grass", "grass");
        play.SelectTile(4, 0);
        Check(play.TravelPathOrNull != null && play.TravelPathOrNull.Count == 4 && !play.IsTraveling, "처음 고른 칸은 경로만 보여 준다.");
        Check(play.Session.Player.X == 0, "경로를 보는 동안에는 움직이지 않는다.");
        play.SelectTile(4, 0);
        Check(play.IsTraveling, "같은 칸을 다시 고르면 걷기 시작한다.");
        RunTravel(play);
        Check(play.Session.Player.X == 4 && play.TravelPathOrNull == null, "목표 칸에 도착하면 멈춘다.");
        Check(play.GetCommandText() == "dddd", "자동 이동의 걸음은 이동 명령으로 기록된다.");
    }

    private static void CheckMenuTravel(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "grass", "grass", "gold");
        List<MenuChoice> choices = new List<MenuChoice>();
        play.CollectMenuChoices(3, 0, choices);
        play.StartMenuChoice(3, 0, choices[0].InteractionOrNull);
        Check(play.IsTraveling, "메뉴에서 고른 일은 바로 걷기 시작한다.");
        RunTravel(play);
        Check(play.Session.Player.Gold > 0 && play.Session.Map.GetItemAtOrNull(3, 0) == null, "닿으면 고른 일(줍기)을 한다.");
        Check(play.GetCommandText() == "ddde", "가서 하는 일도 키보드 명령으로 기록된다.");
    }

    private static void CheckAlerts(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "grass", "grass", "slime");
        EventRecorder recorder = new EventRecorder(play.Session);
        play.Execute(EPlayerCommand.Wait);
        play.Execute(EPlayerCommand.Wait);
        Check(recorder.Alerts.Count == 1 && recorder.Alerts[0] == EAlertKind.Hostile, "적이 플레이어를 알아채면 한 번 알린다.");
        play.Execute(EPlayerCommand.Wait);
        play.Execute(EPlayerCommand.Wait);
        Check(recorder.Alerts.Count == 1, "계속 쫓는 동안에는 다시 알리지 않는다.");
    }

    private static void CheckTownIntro(GameContent content)
    {
        PlaySession play = PlaySession.CreateTown(content, 1);
        EventRecorder recorder = new EventRecorder(play.Session);
        Check(recorder.Messages.Count == 0, "마을을 만들기만 해서는 시작 문구가 나가지 않는다.");
        play.AnnounceIntro();
        play.AnnounceIntro();
        Check(recorder.Messages.Count == 1, "시작 문구는 한 번만 나간다.");
    }

    private static void CheckSameSeedSameGame(GameContent content)
    {
        const string COMMANDS = "aaasssssssssdddddeddddddd..";
        string first = PlayTown(content, 1, COMMANDS);
        string second = PlayTown(content, 1, COMMANDS);
        Check(first == second, "같은 시드와 같은 입력이면 같은 판이 된다.");
    }

    private static string PlayTown(GameContent content, int seed, string commands)
    {
        PlaySession play = PlaySession.CreateTown(content, seed);
        EventRecorder recorder = new EventRecorder(play.Session);
        play.AnnounceIntro();
        for (int index = 0; index < commands.Length; index++)
        {
            play.Execute(PlayerCommandSymbols.GetCommand(commands[index]));
        }

        Check(play.GetCommandText() == commands, "입력 기록이 넣은 명령과 같다.");
        using (Utf16ValueStringBuilder builder = ZString.CreateStringBuilder())
        {
            builder.Append(play.Session.World.Tick);
            foreach (Actor actor in play.Session.Map.Actors)
            {
                builder.Append(ZString.Format("|{0}:{1},{2},{3}", actor.ID, actor.X, actor.Y, actor.Hp));
            }

            builder.Append(ZString.Join("|", recorder.Messages));
            return builder.ToString();
        }
    }

    private static void RunTravel(PlaySession play)
    {
        for (int step = 0; step < 100 && play.IsTraveling; step++)
        {
            play.StepTravel();
        }

        Check(!play.IsTraveling, "자동 이동은 끝난다.");
    }

    private static PlaySession CreatePlay(GameContent content, GameTables tables, params string[] legendIds)
    {
        using (Utf16ValueStringBuilder builder = ZString.CreateStringBuilder())
        {
            for (int index = 0; index < legendIds.Length; index++)
            {
                builder.Append(tables.TownLegend.Get(legendIds[index]).Symbol);
            }

            GameSession session = TownMap.Create(content, new Random(1), new string[] { builder.ToString() });
            return new PlaySession(session);
        }
    }

    private sealed class EventRecorder : IEventListener<MessageLogged>, IEventListener<MoveBlocked>, IEventListener<ActorNoticedPlayer>
    {
        public List<string> Messages { get; } = new List<string>();
        public List<EAlertKind> Alerts { get; } = new List<EAlertKind>();
        public int BlockedCount { get; private set; }

        public EventRecorder(GameSession session)
        {
            session.World.Events.Register<MessageLogged, MoveBlocked, ActorNoticedPlayer>(this);
        }

        public void OnEvent(MessageLogged message)
        {
            Messages.Add(message.Text);
        }

        public void OnEvent(MoveBlocked message)
        {
            BlockedCount++;
        }

        public void OnEvent(ActorNoticedPlayer message)
        {
            Alerts.Add(message.Kind);
        }

        public bool HasMessageContaining(string text)
        {
            for (int index = 0; index < Messages.Count; index++)
            {
                if (Messages[index].Contains(text, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
