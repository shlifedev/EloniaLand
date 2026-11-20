using System;

namespace Elonia.Simulation
{
    public sealed class GameRunner
    {
        public const int READY_ENERGY = 100;

        private int _turnIndex;

        public World World { get; }
        public Actor Player { get; }
        public bool IsGameOver => !Player.IsAlive;

        public GameRunner(World world, Actor player)
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
            RunUntilPlayerTurn();
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

        public static long GetTicksUntilReady(Actor actor)
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

            int speed = actor.Def.Speed;
            return (missingEnergy + speed - 1) / speed;
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
                World.AdvanceTick();
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
