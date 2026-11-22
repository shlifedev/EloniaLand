using System;
using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Commands;
using Elonia.Data;
using Elonia.Events;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
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
        CheckCommandText(content, tables);
    }

    private static void CheckBumpAttack(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "slime");
        EventRecorder recorder = new EventRecorder(play.Session);
        Actor slime = play.Session.Map.Actors[1];
        Check(play.Execute(new MoveCommand(EDirection.Right)), "적이 있는 쪽으로 움직이면 행동이 실행된다.");
        Check(play.Session.Player.X == 0 && slime.Hp < slime.MaxHp, "적이 있는 쪽으로 움직이면 이동 대신 공격한다.");
        Check(recorder.Messages.Count == 1 && recorder.Messages[0] == $"슬라임에게 {play.Session.Player.Stat(EStat.Attack)}의 피해를 주었다.",
            "공격 문장이 로그 이벤트로 한 번 나간다.");
        Check(play.GetCommandText() == "d", "부딪히기 공격도 이동 명령으로 기록된다.");

        for (int index = 0; index < ActorAction.DEFAULT_COST && play.Session.Player.Hp == play.Session.Player.MaxHp; index++)
        {
            play.Execute(new WaitCommand());
        }

        Check(recorder.Messages[^1] == $"슬라임의 공격! {slime.Stat(EStat.Attack)}의 피해를 입었다.", "맞은 문장이 공격자와 피해량을 적는다.");
    }

    private static void CheckBumpHarvest(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "tree");
        EventRecorder recorder = new EventRecorder(play.Session);
        long tick = play.Session.World.Tick;
        Check(play.Execute(new MoveCommand(EDirection.Right)), "나무 쪽으로 움직이면 행동이 실행된다.");
        Check(play.Session.Map.GetPropOrNull(1, 0) == null && play.Session.Player.X == 0, "나무 쪽으로 움직이면 이동 대신 벤다.");
        Check(play.Session.Player.ActionCount == HarvestAction.TURNS && play.Session.World.Tick == tick + HarvestAction.TURNS,
            "명령 하나로 여러 차례 작업을 끝까지 한다(속도 100이면 차례마다 1틱).");
        Check(play.GetCommandText() == "d", "여러 차례 작업도 명령 하나로 기록된다.");
        Check(recorder.HasMessageContaining("나무를 베었다"), "채집 문장이 로그 이벤트로 나간다.");
        CheckInterruptedHarvest(content, tables);
    }

    private static void CheckInterruptedHarvest(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "tree", "grass", "grass", "grass", "grass", "grass", "bat");
        EventRecorder recorder = new EventRecorder(play.Session);
        Actor hero = play.Session.Player;
        Check(play.Execute(new MoveCommand(EDirection.Right)), "캐기를 시작한다.");
        Check(hero.ActionCount == 2 && play.Session.Map.GetPropOrNull(1, 0) != null && play.Session.Map.Things.Count == 0,
            "위협이 나타나면 마지막 차례 전에 멈추고 나무도 수확물도 그대로다.");
        Check(recorder.HasMessageContaining("하던 일을 멈췄다. 근처에 몬스터가 있다."), "멈춘 이유를 로그로 알린다.");
    }

    private static void CheckBlockedMove(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "grass");
        EventRecorder recorder = new EventRecorder(play.Session);
        long tick = play.Session.World.Tick;
        Check(!play.Execute(new MoveCommand(EDirection.Left)), "맵 밖으로는 움직일 수 없다.");
        Check(recorder.BlockedCount == 1 && play.Session.World.Tick == tick, "막힌 쪽으로 움직이면 막힌 이동만 알리고 시간이 흐르지 않는다.");
        Check(!play.Execute(new PickUpCommand()) && recorder.HasMessageContaining("여기에는 주울 것이 없다"), "주울 것이 없으면 알린다.");    }

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
        Check(play.GetCommandText() == "ddd[3,0,pick_up]", "가서 하는 일은 칸과 행동 ID로 기록된다.");
    }

    private static void CheckAlerts(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "grass", "grass", "slime");
        EventRecorder recorder = new EventRecorder(play.Session);
        play.Execute(new WaitCommand());
        play.Execute(new WaitCommand());
        Check(recorder.Alerts.Count == 1 && recorder.Alerts[0] == EAlertKind.Hostile, "적이 플레이어를 알아채면 한 번 알린다.");
        play.Execute(new WaitCommand());
        play.Execute(new WaitCommand());
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

    private static void CheckCommandText(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "villager");
        Talk(play);
        play.Execute(new ChooseCommand(0));
        play.Execute(new ChooseCommand(0));
        play.Execute(new WaitCommand());
        string text = play.GetCommandText();
        Check(text == "[1,0,talk]11.", $"말 걸기, 대답 두 번, 대기가 차례로 기록된다. 기록: {text}");

        PlaySession replayed = CreatePlay(content, tables, "hero", "villager");
        CommandRecord<PlaySession> script = PlayerCommandText.Parse(text);
        for (int index = 0; index < script.Count; index++)
        {
            replayed.Replay(script, index);
        }

        Check(replayed.Session.World.Tick == play.Session.World.Tick && replayed.Session.Flags.Get(Dialogues.PROMISED_HELP_FLAG) == 1,
            "재현 문자열을 새 판에 넣으면 같은 틱과 같은 스토리 플래그가 된다.");
        Check(replayed.GetCommandText() == text, "재현한 판의 기록도 같은 문자열이다.");

        Check(PlayerCommandText.Parse(" w a s d . e ").Count == 6, "명령 사이의 공백은 건너뛴다.");
        Check(PlayerCommandText.Parse("123456789[-1,20,attack]").Count == 10, "대답 번호는 1~9, 칸 좌표는 음수도 읽는다.");
        bool bRejected = false;
        try
        {
            PlayerCommandText.Parse("dd?");
        }
        catch (FormatException)
        {
            bRejected = true;
        }

        Check(bRejected, "모르는 글자가 있으면 FormatException을 던진다.");
        bRejected = false;
        try
        {
            PlayerCommandText.Parse("[1,0,]");
        }
        catch (FormatException)
        {
            bRejected = true;
        }

        Check(bRejected, "행동 ID가 빠진 칸의 일은 FormatException을 던진다.");
    }

    private static string PlayTown(GameContent content, int seed, string commands)
    {
        PlaySession play = PlaySession.CreateTown(content, seed);
        EventRecorder recorder = new EventRecorder(play.Session);
        play.AnnounceIntro();
        CommandRecord<PlaySession> script = PlayerCommandText.Parse(commands);
        for (int index = 0; index < script.Count; index++)
        {
            play.Replay(script, index);
        }

        Actor player = play.Session.Player;
        Check(play.Session.World.Tick == 30 && player.X == 14 && player.Y == 26, "기준 입력의 마지막 틱과 위치가 일치한다.");
        Check(player.Hp == 12 && player.MaxHp == 12, "기준 입력을 마치면 체력이 12/12다.");
        Check(recorder.Messages.Count == 4
            && recorder.Messages[0] == "마을의 조용한 하루. 동쪽 들판에서 무언가 꿈틀거린다."
            && recorder.Messages[1] == "박쥐를 쓰러뜨렸다."
            && recorder.Messages[2] == "물약을 마셨다. (체력 +5)"
            && recorder.Messages[3] == "나무를 베었다. 나뭇가지가 떨어졌다.", $"기준 입력의 로그 네 줄과 순서가 일치한다. 실제 로그: {ZString.Join(" | ", recorder.Messages)}");
        Check(play.GetPromptOrNull() == "E  나뭇가지 줍기", "마지막 위치에서 나뭇가지 줍기를 안내한다.");
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

            GameSession session = TownMap.Create(content, 1, new string[] { builder.ToString() });
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
