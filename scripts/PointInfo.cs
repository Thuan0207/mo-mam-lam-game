using Godot;

public class PointInfo
{
    public bool IsLeftWall;
    public bool IsRightWall;
    public bool IsFallTile; // the tile that can be fall into
    public bool IsRightEdge;
    public bool IsLeftEdge;
    public bool IsPositionPoint;
    public Vector2 Position;
    public long PointId;

    public PointInfo() { }

    public PointInfo(long pointId, Vector2 position)
    {
        PointId = pointId;
        Position = position;
    }
}
