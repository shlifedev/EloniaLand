using Elonia.Data;
using Elonia.Simulation;

public class Program
{
    internal sealed class HeroDef : IActorDef
    {
        public string Id => "hero";
    }
    public class HandsOnStats : IActorStats
    {
        public int Health { get; set; } = 100;
        public int Mana { get; set; } = 50;
        public int Strength { get; set; } = 10;
        public int Agility { get; set; } = 10;
        public int Intelligence { get; set; } = 10;

        public int MaxHp => 100;

        public int Speed => 100;
    }

    public class RestAction : ActorAction
    {
        public static readonly RestAction DEFAULT = new RestAction();
        private RestAction() { }

        public override string Id => "rest";

        public override ActionResult Execute(ActionContext context)
        {
            Actor actor = context.Actor;
            int healAmount = 5;
            if (actor.Hp + healAmount > actor.MaxHp)
            {
                return ActionResult.REJECTED;
            }

            actor.Heal(healAmount);
            return ActionResult.Executed(DEFAULT_COST);
        }
    }

    public static void Main(string[] args)
    {
        string tablesPath = Path.Combine(AppContext.BaseDirectory, "Tables");
        HeroDef def = new HeroDef();
        Actor actor = new Actor(1, def, new HandsOnStats(), 0, 0);
        Map map = new Map(128, 128);
        map.AddEntity(actor);
        World world = new World(map);
        GameRunner runner = new GameRunner(world, actor);

        while (true)
        {
            actor.TakeDamage(5);
            Console.WriteLine($"hp : " + actor.Hp);
            runner.SubmitPlayerAction(RestAction.DEFAULT);
            Console.WriteLine($"hp : " + actor.Hp);
            Console.WriteLine($"ticked : " + world.Tick);
            Thread.Sleep(1000);
        }
    }
}
