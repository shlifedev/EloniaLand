using System;
using System.Collections.Generic;

namespace Elonia.Simulation
{
    public sealed class GameRunner
    {
        public const int READY_ENERGY = 100;

        public const int MAX_NPC_SPEED_RATIO = 10;

        private int _turnIndex;

        public World World { get; }
        public Actor Player { get; }
        public bool IsGameOver => !Player.IsAlive;

        public int TurnIndex => _turnIndex;

        public GameRunner(World world, Actor player)
            : this(world, player, 0)
        {
            RunUntilPlayerTurn();
        }

        private GameRunner(World world, Actor player, int turnIndex)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (!world.CurrentMap.Contains(player))
            {
                throw new ArgumentException("The player must be in the current map.", nameof(player));
            }

            if (player.ControllerOrNull != null)
            {
                throw new ArgumentException("The player acts on input and must not have a controller.", nameof(player));
            }

            World = world;
            Player = player;
            _turnIndex = turnIndex;
        }

        public static GameRunner Restore(World world, Actor player, int turnIndex)
        {
            GameRunner runner = new GameRunner(world, player, turnIndex);
            IReadOnlyList<Actor> actors = world.CurrentMap.Actors;
            if (turnIndex < 0 || turnIndex >= actors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            }

            if (player.IsAlive && (actors[turnIndex] != player || player.Energy < READY_ENERGY))
            {
                throw new ArgumentException("저장한 차례가 플레이어의 입력 대기 상태와 다릅니다.", nameof(turnIndex));
            }

            return runner;
        }

        public ActionResult SubmitPlayerAction(ActorAction action)
        {
            if (IsGameOver)
            {
                return ActionResult.REJECTED;
            }

            ActionResult result = ActionExecutor.Execute(action, new ActionContext(Player, World.CurrentMap, World));
            if (result.IsExecuted)
            {
                RunUntilPlayerTurn();
            }

            return result;
        }

        public long GetEnergyPerTick(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            long speed = actor.Speed;
            if (actor == Player)
            {
                return speed;
            }

            long maxNpcSpeed = (long)Player.Speed * MAX_NPC_SPEED_RATIO;
            return Math.Min(speed, maxNpcSpeed);
        }

        public long GetTicksUntilReady(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            long missingEnergy = READY_ENERGY - actor.Energy;
            if (missingEnergy <= 0)
            {
                return 0;
            }

            long energyPerTick = GetEnergyPerTick(actor);
            return (missingEnergy + energyPerTick - 1) / energyPerTick;
        }

        private void RunUntilPlayerTurn()
        {
            while (Player.IsAlive)
            {
                Map map = World.CurrentMap;
                while (_turnIndex < map.Actors.Count)
                {
                    Actor actor = map.Actors[_turnIndex];
                    if (!actor.IsAlive || actor.Energy < READY_ENERGY)
                    {
                        _turnIndex++;
                        continue;
                    }

                    if (actor == Player)
                    {
                        return;
                    }

                    RunNpcTurn(actor);
                    if (!Player.IsAlive)
                    {
                        return;
                    }
                }

                _turnIndex = 0;
                AdvanceTick();
            }
        }

        private void AdvanceTick()
        {
            World.AdvanceTick();
            IReadOnlyList<Actor> actors = World.CurrentMap.Actors;
            for (int index = 0; index < actors.Count; index++)
            {
                Actor actor = actors[index];
                if (actor.IsAlive)
                {
                    actor.AddEnergy(GetEnergyPerTick(actor));
                }
            }
        }

        private void RunNpcTurn(Actor npc)
        {
            IActorController? controllerOrNull = npc.ControllerOrNull;
            if (controllerOrNull == null)
            {
                throw new InvalidOperationException($"NPC {npc.ID} has no controller.");
            }

            ActionContext context = new ActionContext(npc, World.CurrentMap, World);
            ActorAction action = controllerOrNull.ChooseAction(context);
            ActionResult result = ActionExecutor.Execute(action, context);
            if (!result.IsExecuted)
            {
                ActionExecutor.Execute(WaitAction.DEFAULT, context);
            }
        }
    }
}
