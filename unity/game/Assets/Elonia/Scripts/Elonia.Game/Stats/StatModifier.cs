namespace Elonia.Game
{
    public readonly struct StatModifier
    {
        public string Source { get; }
        public EStat Stat { get; }
        public int Amount { get; }

        public StatModifier(string source, EStat stat, int amount)
        {
            Source = source;
            Stat = stat;
            Amount = amount;
        }
    }
}
