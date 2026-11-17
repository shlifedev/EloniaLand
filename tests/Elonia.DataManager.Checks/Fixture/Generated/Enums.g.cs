#nullable enable

using System;

namespace Elonia.Checks.Generated
{
    [Flags]
    public enum EAccess : int
    {
        None = 0,
        Read = 1,
        Write = 2,
        All = 3,
    }

    public enum EKind : byte
    {
        None = 0,
        Potion = 1,
        Gold = 10,
    }

    public enum EMonsterId : int
    {
        Slime = 1001,
        Bat = 1002,
        Dragon = 2001,
    }

    public enum EPlain : int
    {
        A = 0,
        B = 1,
        C = 2,
    }

    public enum ESmall : short
    {
        Low = -1,
        High = 0,
    }
}
