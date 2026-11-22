namespace Elonia.Simulation
{
    public readonly struct ItemStack
    {
        public IThingDef Def { get; }
        public int Count { get; }

        public ItemStack(IThingDef def, int count)
        {
            Def = def;
            Count = count;
        }
    }
}
