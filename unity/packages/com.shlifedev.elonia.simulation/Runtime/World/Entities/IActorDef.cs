namespace Elonia.Simulation
{
    public interface IActorDef
    {
        string Id { get; }
        int MaxHp { get; }
        int Speed { get; }
    }
}
