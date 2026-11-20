using System;
using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class PlaySession
    {
        private const string TOWN_INTRO = "마을의 조용한 하루. 동쪽 들판에서 무언가 꿈틀거린다.";

        private readonly PlayerTravel _travel = new PlayerTravel();

        private readonly InteractionPlan _plan = new InteractionPlan(new AttackRule(), new HarvestRule(), new PickUpRule());

        private readonly List<char> _commandSymbols = new List<char>();

        private readonly Dictionary<Actor, IBehavior?> _lastBehaviors = new Dictionary<Actor, IBehavior?>();
        private string? _commandTextOrNull;

        private string? _introOrNull;

        public GameSession Session { get; }
        public bool IsGameOver => Session.Runner.IsGameOver;

        public bool IsTraveling => _travel.IsActive;

        public IReadOnlyList<(int X, int Y)>? TravelPathOrNull => _travel.IsActive || _travel.IsPreviewing ? _travel.Path : null;

        public int CommandCount => _commandSymbols.Count;

        public PlaySession(GameSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            session.World.Events.Register<ActorAttacked, ActorDied, ItemPickedUp, PropHarvested, StepTriggered, ActorDamaged>(
                new MessageFeed(session.Player, session.World.Events));
        }

        public static PlaySession CreateTown(GameContent content, int seed)
        {
            PlaySession play = new PlaySession(TownMap.Create(content, new Random(seed)));
            play._introOrNull = TOWN_INTRO;
            return play;
        }

        public void AnnounceIntro()
        {
            if (_introOrNull == null)
            {
                return;
            }

            Log(_introOrNull, ELogTone.Muted);
            _introOrNull = null;
        }

        public ActionResult Execute(EPlayerCommand command)
        {
            if (command == EPlayerCommand.None)
            {
                return ActionResult.REJECTED;
            }

            _travel.Cancel();
            return ExecuteCommand(command);
        }

        public void SelectTile(int x, int y)
        {
            if (IsGameOver || !Session.Map.IsInside(x, y))
            {
                return;
            }

            if (_travel.IsPreviewGoal(x, y))
            {
                _travel.Confirm(Session);
                return;
            }

            _travel.Cancel();
            _travel.Preview(Session, x, y, GetBumpInteractionOrNull(x, y));
        }

        public void CancelTravel()
        {
            _travel.Cancel();
        }

        public void CollectMenuChoices(int x, int y, List<MenuChoice> choices)
        {
            choices.Clear();
            Map map = Session.Map;
            if (IsGameOver || !map.IsInside(x, y))
            {
                return;
            }

            Actor player = Session.Player;
            _plan.Collect(map, player, x, y);
            for (int index = 0; index < _plan.Items.Count; index++)
            {
                Interaction interaction = _plan.Items[index];
                choices.Add(new MenuChoice(GameText.GetInteractionLabel(interaction, map), interaction));
            }

            bool bPlayerTile = x == player.X && y == player.Y;
            if (!bPlayerTile)
            {
                choices.Add(new MenuChoice("여기로 걸어가기", null));
            }
        }

        public void StartMenuChoice(int x, int y, Interaction? interactionOrNull)
        {
            if (!_travel.Start(Session, x, y, interactionOrNull))
            {
                Log("그곳에는 갈 수 없다.", ELogTone.Muted);
            }
        }

        public void StepTravel()
        {
            ETravelStep step = _travel.NextStep(Session, out EPlayerCommand moveCommand);
            if (step == ETravelStep.None)
            {
                return;
            }

            if (step == ETravelStep.Interact)
            {
                Interaction interaction = _travel.Interaction;
                _travel.Cancel();
                PerformInteraction(interaction);
                return;
            }

            ActionResult result = ExecuteCommand(moveCommand);
            if (!result.IsExecuted)
            {
                _travel.Cancel();
                return;
            }

            string? reasonOrNull = _travel.CheckInterruptOrNull(Session);
            if (reasonOrNull != null)
            {
                Log(reasonOrNull, ELogTone.Danger);
            }
        }

        public string? GetPromptOrNull()
        {
            Actor player = Session.Player;
            Thing? itemOrNull = Session.Map.GetItemAtOrNull(player.X, player.Y);
            if (itemOrNull != null)
            {
                return ZString.Concat("E  ", GameText.GetPickUpLabel(itemOrNull.Row()));
            }

            return null;
        }

        public string GetCommandText()
        {
            if (_commandTextOrNull == null)
            {
                using (Utf16ValueStringBuilder builder = ZString.CreateStringBuilder())
                {
                    for (int index = 0; index < _commandSymbols.Count; index++)
                    {
                        builder.Append(_commandSymbols[index]);
                    }

                    _commandTextOrNull = builder.ToString();
                }
            }

            return _commandTextOrNull;
        }

        private ActionResult ExecuteCommand(EPlayerCommand command)
        {
            if (command == EPlayerCommand.None || IsGameOver)
            {
                return ActionResult.REJECTED;
            }

            RecordCommand(command);

            ActionResult result;
            switch (command)
            {
                case EPlayerCommand.MoveUp:
                    result = MoveOrInteract(MoveAction.UP);
                    break;
                case EPlayerCommand.MoveDown:
                    result = MoveOrInteract(MoveAction.DOWN);
                    break;
                case EPlayerCommand.MoveLeft:
                    result = MoveOrInteract(MoveAction.LEFT);
                    break;
                case EPlayerCommand.MoveRight:
                    result = MoveOrInteract(MoveAction.RIGHT);
                    break;
                case EPlayerCommand.PickUp:
                    result = Session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT);
                    if (!result.IsExecuted)
                    {
                        Log("여기에는 주울 것이 없다.", ELogTone.Muted);
                    }
                    break;
                case EPlayerCommand.Wait:
                    result = Session.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
                    break;
                default:
                    return ActionResult.REJECTED;
            }

            if (result.IsExecuted)
            {
                NotifyAlerts();
            }

            return result;
        }

        private void RecordCommand(EPlayerCommand command)
        {
            char? symbolOrNull = PlayerCommandSymbols.GetSymbolOrNull(command);
            if (symbolOrNull != null)
            {
                _commandSymbols.Add(symbolOrNull.Value);
                _commandTextOrNull = null;
            }
        }

        private Interaction? GetBumpInteractionOrNull(int x, int y)
        {
            if (_plan.TryCollectBump(Session.Map, Session.Player, x, y, out Interaction interaction))
            {
                return interaction;
            }

            return null;
        }

        private void PerformInteraction(Interaction interaction)
        {
            EPlayerCommand command = GetKeyboardCommand(interaction);
            if (command != EPlayerCommand.None)
            {
                ExecuteCommand(command);
                return;
            }

            ActionResult result = Session.Runner.SubmitPlayerAction(interaction.Action);
            if (result.IsExecuted)
            {
                NotifyAlerts();
            }
        }

        private EPlayerCommand GetKeyboardCommand(Interaction interaction)
        {
            Actor player = Session.Player;
            if (interaction.Action is PickUpAction)
            {
                return EPlayerCommand.PickUp;
            }

            if (interaction.Reach != 1)
            {
                return EPlayerCommand.None;
            }

            Interaction? bumpOrNull = GetBumpInteractionOrNull(interaction.TargetX, interaction.TargetY);
            if (bumpOrNull == null || bumpOrNull.Value.Action.GetType() != interaction.Action.GetType())
            {
                return EPlayerCommand.None;
            }

            int deltaX = interaction.TargetX - player.X;
            int deltaY = interaction.TargetY - player.Y;
            if (deltaX != 0)
            {
                return deltaX < 0 ? EPlayerCommand.MoveLeft : EPlayerCommand.MoveRight;
            }

            return deltaY < 0 ? EPlayerCommand.MoveUp : EPlayerCommand.MoveDown;
        }

        private ActionResult MoveOrInteract(MoveAction move)
        {
            Actor player = Session.Player;
            Interaction? interactionOrNull = GetBumpInteractionOrNull(player.X + move.DeltaX, player.Y + move.DeltaY);
            if (interactionOrNull != null)
            {
                return Session.Runner.SubmitPlayerAction(interactionOrNull.Value.Action);
            }

            ActionResult result = Session.Runner.SubmitPlayerAction(move);
            if (!result.IsExecuted)
            {
                Session.World.Events.Broadcast(new MoveBlocked(player, move.DeltaX, move.DeltaY));
            }

            return result;
        }

        private void NotifyAlerts()
        {
            IReadOnlyList<Actor> actors = Session.Map.Actors;
            for (int index = 0; index < actors.Count; index++)
            {
                Actor actor = actors[index];
                if (!actor.IsAlive || !(actor.ControllerOrNull is Brain brain))
                {
                    continue;
                }

                IBehavior? current = brain.CurrentBehaviorOrNull;
                _lastBehaviors.TryGetValue(actor, out IBehavior? lastOrNull);
                if (IsAlert(current) && !IsAlert(lastOrNull))
                {
                    EAlertKind kind = current is FleeBehavior ? EAlertKind.Scared : EAlertKind.Hostile;
                    Session.World.Events.Broadcast(new ActorNoticedPlayer(actor, kind));
                }

                _lastBehaviors[actor] = current;
            }
        }

        private void Log(string text, ELogTone tone)
        {
            Session.World.Events.Broadcast(new MessageLogged(text, tone));
        }

        private static bool IsAlert(IBehavior? behaviorOrNull)
        {
            return behaviorOrNull is ChaseBehavior || behaviorOrNull is AttackBehavior || behaviorOrNull is FleeBehavior;
        }
    }
}
