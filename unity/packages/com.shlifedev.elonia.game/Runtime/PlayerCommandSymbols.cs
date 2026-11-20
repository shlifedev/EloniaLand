using System;

namespace Elonia.Game
{
    public static class PlayerCommandSymbols
    {
        public static char? GetSymbolOrNull(EPlayerCommand command)
        {
            switch (command)
            {
                case EPlayerCommand.MoveUp:
                    return 'w';
                case EPlayerCommand.MoveDown:
                    return 's';
                case EPlayerCommand.MoveLeft:
                    return 'a';
                case EPlayerCommand.MoveRight:
                    return 'd';
                case EPlayerCommand.Wait:
                    return '.';
                case EPlayerCommand.PickUp:
                    return 'e';
                default:
                    return null;
            }
        }

        public static EPlayerCommand GetCommand(char symbol)
        {
            switch (symbol)
            {
                case 'w':
                    return EPlayerCommand.MoveUp;
                case 's':
                    return EPlayerCommand.MoveDown;
                case 'a':
                    return EPlayerCommand.MoveLeft;
                case 'd':
                    return EPlayerCommand.MoveRight;
                case '.':
                    return EPlayerCommand.Wait;
                case 'e':
                    return EPlayerCommand.PickUp;
                default:
                    throw new ArgumentException($"Unknown command symbol '{symbol}'.", nameof(symbol));
            }
        }
    }
}
