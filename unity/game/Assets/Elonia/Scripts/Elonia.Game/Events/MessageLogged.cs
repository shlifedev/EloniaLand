namespace Elonia.Game
{
    public readonly struct MessageLogged
    {
        public string Text { get; }
        public ELogTone Tone { get; }

        public MessageLogged(string text, ELogTone tone)
        {
            Text = text;
            Tone = tone;
        }
    }
}
