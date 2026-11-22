using Elonia.Commands;
using Elonia.Data;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
{
    private static void CheckDiagonals(GameContent content)
    {
        CheckDiagonalSteps(content);
        CheckCornerCutting(content);
        CheckDiagonalBumps(content);
        CheckDiagonalTravel(content);
        CheckDiagonalCommandText(content);
    }

    private static void CheckDiagonalSteps(GameContent content)
    {
        PlaySession play = new PlaySession(CreateMap(content, "@..", "...", "..."));
        Actor hero = play.Session.Player;
        long tick = play.Session.World.Tick;
        Check(play.Execute(new MoveCommand(EDirection.DownRight)) && hero.X == 1 && hero.Y == 1, "대각선으로 한 칸 움직인다.");
        Check(play.Session.World.Tick == tick + ActorAction.DEFAULT_COST / hero.Speed, "대각선 이동도 상하좌우 이동과 같은 시간을 쓴다.");
        Check(play.Execute(new MoveCommand(EDirection.UpRight)) && hero.X == 2 && hero.Y == 0
            && play.Execute(new MoveCommand(EDirection.DownLeft)) && hero.X == 1 && hero.Y == 1
            && play.Execute(new MoveCommand(EDirection.UpLeft)) && hero.X == 0 && hero.Y == 0,
            "네 대각선 방향으로 모두 움직인다.");
    }

    private static void CheckCornerCutting(GameContent content)
    {
        PlaySession wallBeside = new PlaySession(CreateMap(content, "@w", ".."));
        EventRecorder recorder = new EventRecorder(wallBeside.Session);
        long tick = wallBeside.Session.World.Tick;
        Check(!wallBeside.Execute(new MoveCommand(EDirection.DownRight)) && wallBeside.Session.Player.X == 0 && wallBeside.Session.Player.Y == 0,
            "옆 칸이 벽이면 대각선으로 모서리를 질러갈 수 없다.");
        Check(recorder.BlockedCount == 1 && wallBeside.Session.World.Tick == tick, "모서리에 막힌 대각선 이동은 막힌 이동만 알리고 시간이 흐르지 않는다.");

        PlaySession wallBelow = new PlaySession(CreateMap(content, "@.", "w."));
        Check(!wallBelow.Execute(new MoveCommand(EDirection.DownRight)) && wallBelow.Session.Player.X == 0,
            "아래 칸이 벽이어도 대각선으로 질러갈 수 없다.");

        GameSession crowded = CreateMap(content, "@.", "..");
        ActorRow villagerRow = content.Tables.Actor.Get("villager");
        crowded.Map.AddEntity(new Actor(crowded.Map.CreateEntityID(), villagerRow, new ActorStats(villagerRow), 1, 0, new WaitingController()));
        PlaySession pastActor = new PlaySession(crowded);
        Check(pastActor.Execute(new MoveCommand(EDirection.DownRight)) && crowded.Player.X == 1 && crowded.Player.Y == 1,
            "옆 칸의 액터는 대각선 이동을 막지 않는다.");
    }

    private static void CheckDiagonalBumps(GameContent content)
    {
        PlaySession attack = new PlaySession(CreateMap(content, "@w", "ws"));
        Actor slime = attack.Session.Map.Actors[1];
        Check(attack.Execute(new MoveCommand(EDirection.DownRight)) && attack.Session.Player.X == 0 && slime.Hp < slime.MaxHp,
            "대각선의 적에게 부딪히면 모서리가 벽이어도 공격한다.");

        PlaySession harvest = new PlaySession(CreateMap(content, "@.", ".T"));
        Check(harvest.Execute(new MoveCommand(EDirection.DownRight)) && harvest.Session.Map.GetPropOrNull(1, 1) == null
            && harvest.Session.Player.X == 0, "대각선의 나무에 부딪히면 이동 대신 벤다.");
    }

    private static void CheckDiagonalTravel(GameContent content)
    {
        PlaySession play = new PlaySession(CreateMap(content, "@...", "....", "...."));
        play.SelectTile(2, 2);
        Check(play.TravelPathOrNull != null && play.TravelPathOrNull.Count == 2, "대각선 목표까지의 경로는 대각선 걸음 수만큼이다.");
        play.SelectTile(2, 2);
        RunTravel(play);
        Check(play.Session.Player.X == 2 && play.Session.Player.Y == 2 && play.GetCommandText() == "nn", "자동 이동도 대각선으로 걷고 그 걸음을 기록한다.");
    }

    private static void CheckDiagonalCommandText(GameContent content)
    {
        PlaySession play = new PlaySession(CreateMap(content, "...", ".@.", "..."));
        play.Execute(new MoveCommand(EDirection.UpLeft));
        play.Execute(new MoveCommand(EDirection.DownRight));
        play.Execute(new MoveCommand(EDirection.UpRight));
        play.Execute(new MoveCommand(EDirection.DownLeft));
        string text = play.GetCommandText();
        Check(text == "ynub", $"대각선 이동은 y(왼쪽 위), u(오른쪽 위), b(왼쪽 아래), n(오른쪽 아래)으로 기록된다. 기록: {text}");

        PlaySession replayed = new PlaySession(CreateMap(content, "...", ".@.", "..."));
        CommandRecord<PlaySession> script = PlayerCommandText.Parse("y");
        replayed.Replay(script, 0);
        Check(replayed.Session.Player.X == 0 && replayed.Session.Player.Y == 0 && replayed.GetCommandText() == "y",
            "대각선 기록을 다시 읽으면 같은 칸으로 움직이고 같은 문자열로 기록된다.");
    }
}
