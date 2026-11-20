using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct MenuChoice
    {
        public string Label { get; }

        public Interaction? InteractionOrNull { get; }

        public MenuChoice(string label, Interaction? interactionOrNull)
        {
            Label = label;
            InteractionOrNull = interactionOrNull;
        }
    }
}
