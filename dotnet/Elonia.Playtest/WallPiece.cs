namespace Elonia.Playtest;

internal readonly struct WallPiece
{
    public bool IsDoor { get; }
    public bool IsNorthConnected { get; }
    public bool IsSouthConnected { get; }
    public bool IsWestConnected { get; }
    public bool IsEastConnected { get; }

    public bool HasFrontFace => !IsSouthConnected;

    public WallPiece(bool bDoor, bool bNorth, bool bSouth, bool bWest, bool bEast)
    {
        IsDoor = bDoor;
        IsNorthConnected = bNorth;
        IsSouthConnected = bSouth;
        IsWestConnected = bWest;
        IsEastConnected = bEast;
    }
}
