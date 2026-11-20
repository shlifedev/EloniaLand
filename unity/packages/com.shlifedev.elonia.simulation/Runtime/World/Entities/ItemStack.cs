namespace Elonia.Simulation
{
    public readonly struct ItemStack
    {
        public ThingType Type { get; }
        public int Count { get; }

        public ItemStack(ThingType type, int count)
        {
            Type = type;
            Count = count;
        }
    }
}
