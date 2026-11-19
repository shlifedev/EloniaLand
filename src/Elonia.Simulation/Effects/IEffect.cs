namespace Elonia.Simulation
{
    public interface IEffect
    {
        void Apply(ActionContext context, Actor target);
    }
}
