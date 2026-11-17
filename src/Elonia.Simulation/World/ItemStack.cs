namespace Elonia.Simulation
{
    public readonly struct ItemStack
    {
        public EItemType Type { get; }
        public int Count { get; }

        public ItemStack(EItemType type, int count)
        {
            Type = type;
            Count = count;
        }
    }
}
