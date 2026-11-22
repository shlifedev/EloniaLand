using System;
using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Commands;
using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed partial class PlaySession
    {
        private const string TOWN_INTRO = "마을의 조용한 하루. 동쪽 들판에서 무언가 꿈틀거린다.";

        public const int COMMAND_CAPACITY = 1024;

        private readonly PlayerTravel _travel = new PlayerTravel();

        private readonly ThreatWatch _workWatch = new ThreatWatch();

        private readonly CommandRecord<PlaySession> _commands = new CommandRecord<PlaySession>(COMMAND_CAPACITY);

        private readonly HashSet<Actor> _alertActors = new HashSet<Actor>();
        private string? _commandTextOrNull;

        private string? _introOrNull;

        private Actor? _dialogueSpeakerOrNull;
        private DialogueLine? _dialogueLineOrNull;

        public GameSession Session { get; }
        public int Seed => Session.World.Seed;
        public bool IsFromSave { get; private set; }
        internal TileInteractions Interactions { get; }
        public bool IsGameOver => Session.Runner.IsGameOver;

        public bool IsTraveling => _travel.IsActive;

        public IReadOnlyList<(int X, int Y)>? TravelPathOrNull => _travel.IsActive || _travel.IsPreviewing ? _travel.Path : null;

        public int CommandCount => _commands.Count;

        public bool HasUsedCheats { get; private set; }

        public bool IsTalking => _dialogueLineOrNull != null;

        public Actor? DialogueSpeakerOrNull => _dialogueSpeakerOrNull;
        public DialogueLine? DialogueLineOrNull => _dialogueLineOrNull;

        public PlaySession(GameSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Interactions = new TileInteractions(session);
            session.World.Events.Register<ActorDied, ItemPickedUp, PropHarvested, StepTriggered, ActorDamaged>(
                new MessageFeed(session.Player, session.World.Events));
        }

        public static PlaySession CreateTown(GameContent content, int seed)
        {
            PlaySession play = new PlaySession(TownMap.Create(content, seed));
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

        public bool Execute<TCommand>(in TCommand command) where TCommand : struct, IPlayerCommand
        {
            _travel.Cancel();
            return ExecuteAndRecord(command);
        }

        public bool Replay(CommandRecord<PlaySession> commands, int index)
        {
            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            _travel.Cancel();
            if (IsGameOver)
            {
                return false;
            }

            commands.CopyTo(index, _commands);
            _commandTextOrNull = null;
            return commands.Execute(index, this);
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
            _travel.Preview(Session, x, y, Interactions.GetBumpOrNull(x, y));
        }

        public void CancelTravel()
        {
            _travel.Cancel();
        }

        public void CollectMenuChoices(int x, int y, List<MenuChoice> choices)
        {
            Interactions.CollectMenuChoices(x, y, choices);
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
            ETravelStep step = _travel.NextStep(Session, out EDirection direction);
            if (step == ETravelStep.None)
            {
                return;
            }

            if (step == ETravelStep.Interact)
            {
                Interaction interaction = _travel.Interaction;
                _travel.Cancel();
                ExecuteAndRecord(new InteractCommand(interaction.TargetX, interaction.TargetY, interaction.Action.Id));
                return;
            }

            if (!ExecuteAndRecord(new MoveCommand(direction)))
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

        public void MarkCheatUsed()
        {
            HasUsedCheats = true;
        }

        public string GetCommandText()
        {
            if (_commandTextOrNull == null)
            {
                CommandTextWriter writer = new CommandTextWriter();
                _commands.WriteTo(writer);
                _commandTextOrNull = writer.ToString();
            }

            return _commandTextOrNull;
        }

        internal bool SubmitTurn(ActorAction action)
        {
            EndDialogue();
            if (action is IWorkAction work)
            {
                return SubmitWork(action, work);
            }

            return SubmitOneTurn(action);
        }

        private bool SubmitWork(ActorAction action, IWorkAction work)
        {
            _workWatch.Reset(Session, null);
            if (!SubmitOneTurn(action))
            {
                return false;
            }

            while (!work.IsFinished && !IsGameOver)
            {
                _workWatch.Update(Session, out bool bHurt, out bool bThreatAppeared);
                if (bHurt)
                {
                    Log("하던 일을 멈췄다. 무언가 공격해 온다!", ELogTone.Danger);
                    break;
                }

                if (bThreatAppeared)
                {
                    Log("하던 일을 멈췄다. 근처에 몬스터가 있다.", ELogTone.Danger);
                    break;
                }

                if (!SubmitOneTurn(action))
                {
                    break;
                }
            }

            return true;
        }

        private bool SubmitOneTurn(ActorAction action)
        {
            ActionResult result = Session.Runner.SubmitPlayerAction(action);
            if (!result.IsExecuted)
            {
                return false;
            }

            if (action is TalkAction talk)
            {
                StartDialogue(talk.Target);
            }

            NotifyAlerts();
            return true;
        }

        internal void Choose(int index)
        {
            if (_dialogueSpeakerOrNull == null || _dialogueLineOrNull == null || index >= _dialogueLineOrNull.Choices.Count)
            {
                return;
            }

            DialogueChoice choice = _dialogueLineOrNull.Choices[index];
            if (choice.FlagOrNull != null)
            {
                Session.Flags.Set(choice.FlagOrNull, 1);
            }

            if (choice.NextLineOrNull == null)
            {
                EndDialogue();
                return;
            }

            ShowLine(_dialogueSpeakerOrNull, choice.NextLineOrNull);
        }

        internal void Log(string text, ELogTone tone)
        {
            Session.World.Events.Broadcast(new MessageLogged(text, tone));
        }

        private bool ExecuteAndRecord<TCommand>(in TCommand command) where TCommand : struct, IPlayerCommand
        {
            if (IsGameOver)
            {
                return false;
            }

            _commands.Add(command);
            _commandTextOrNull = null;
            return command.Execute(this);
        }

        private void StartDialogue(Actor speaker)
        {
            if (IsGameOver || !speaker.IsAlive)
            {
                return;
            }

            DialogueLine? lineOrNull = Dialogues.GetStartLineOrNull(speaker.Row(), Session.Flags);
            if (lineOrNull != null)
            {
                ShowLine(speaker, lineOrNull);
            }
        }

        private void ShowLine(Actor speaker, DialogueLine line)
        {
            _dialogueSpeakerOrNull = speaker;
            _dialogueLineOrNull = line;
        }

        private void EndDialogue()
        {
            _dialogueSpeakerOrNull = null;
            _dialogueLineOrNull = null;
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
                if (!IsAlert(current))
                {
                    _alertActors.Remove(actor);
                    continue;
                }

                if (_alertActors.Add(actor))
                {
                    EAlertKind kind = current is FleeBehavior ? EAlertKind.Scared : EAlertKind.Hostile;
                    Session.World.Events.Broadcast(new ActorNoticedPlayer(actor, kind));
                }
            }
        }

        private static bool IsAlert(IBehavior? behaviorOrNull)
        {
            return behaviorOrNull is ChaseBehavior || behaviorOrNull is AttackBehavior || behaviorOrNull is FleeBehavior;
        }
    }
}
