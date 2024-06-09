using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class TileMapPathFind : TileMap
{
    [Export]
    public bool ShowDebugGrapth = true;

    [Export]
    public int JumpDistance = 5; // this using map coor

    [Export]
    public int JumpHeight = 2; // this using map coor
    const int COLLISION_LAYER = 0;
    const int EMPTY_CELL = -1; // godot default this to -1

    const int MAX_TILE_FALL_SCAN_DEPTH = 1000; // max number of tiles to scan downward for a solid tiles (!= CELL_IS_EMPTY)

    AStar2D _aStarGrapth = new();
    Array<Vector2I> _usedTiles;

    readonly System.Collections.Generic.List<PointInfo> _pointInfoList = new();

    [Export]
    PackedScene _grapthPoint;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _usedTiles = GetUsedCells(COLLISION_LAYER);
        BuildGrapth();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    public override void _Draw()
    {
        // if (ShowDebugGrapth)
        ConnectPoints();
    }

    void BuildGrapth()
    {
        AddGrapthPoints(); // add all grapth points
    }

    void DrawDebugLine(Vector2 from, Vector2 to, Color color)
    {
        if (ShowDebugGrapth)
            DrawLine(from, to, color);
    }

#nullable enable
    PointInfo? GetPointInfo(Vector2I tile)
    {
        // Loop through the point info list
        foreach (var pointInfo in _pointInfoList)
        {
            // If the tile has been added to the points list
            if (pointInfo.Position == MapToLocal(tile))
            {
                return pointInfo; // Return the PointInfo
            }
        }
        return null; // If the tile wasn't found, return null
    }

    PointInfo? GetPointInfoAtPosition(Vector2 pos)
    {
        PointInfo newPointInfo = new(-10000, pos) { IsPositionPoint = true };
        var tile = LocalToMap(pos);
        // if a tile was found below
        if (GetCellSourceId(COLLISION_LAYER, new(tile.X, tile.Y + 1)) != EMPTY_CELL)
        {
            if (GetCellSourceId(COLLISION_LAYER, new(tile.X + 1, tile.Y)) != EMPTY_CELL)
                newPointInfo.IsRightWall = true;

            if (GetCellSourceId(COLLISION_LAYER, new(tile.X - 1, tile.Y)) != EMPTY_CELL)
                newPointInfo.IsLeftWall = true;

            if (GetCellSourceId(COLLISION_LAYER, new(tile.X - 1, tile.Y + 1)) == EMPTY_CELL)
                newPointInfo.IsLeftEdge = true;

            if (GetCellSourceId(COLLISION_LAYER, new(tile.X + 1, tile.Y + 1)) == EMPTY_CELL)
                newPointInfo.IsRightEdge = true;
        }
        return newPointInfo;
    }

    static System.Collections.Generic.Stack<PointInfo> ReversedPathStack(
        System.Collections.Generic.Stack<PointInfo> pathStack
    )
    {
        System.Collections.Generic.Stack<PointInfo> pathStackReversed = new();
        while (pathStack.Count != 0)
        {
            pathStackReversed.Push(pathStack.Pop());
        }
        return pathStackReversed;
    }

    public System.Collections.Generic.Queue<PointInfo> GetPlatform2DPath(
        Vector2 startPos,
        Vector2 endPos
    )
    {
        System.Collections.Generic.Queue<PointInfo> pathQueue = new();
        long[]? idPath = _aStarGrapth.GetIdPath(
            _aStarGrapth.GetClosestPoint(startPos),
            _aStarGrapth.GetClosestPoint(endPos)
        );
        AddVisualPoint(LocalToMap(startPos), new("red"));
        AddVisualPoint(LocalToMap(endPos), new("black"));
        var startPoint = GetPointInfoAtPosition(startPos);
        var endPoint = GetPointInfoAtPosition(endPos);

        if (idPath.Length <= 0)
            return pathQueue;

        for (int i = 0; i < idPath.Length; ++i)
        {
            PointInfo currPoint = GetPointInfoById(idPath[i]);
            pathQueue.Enqueue(currPoint);
        }

        return pathQueue;
    }

    private PointInfo GetPointInfoById(long id)
    {
        return _pointInfoList.Single(p => p.PointId == id);
    }

    Vector2I? GetStartTileForFallPointScanning(Vector2I tile)
    {
        var tileAbove = new Vector2I(tile.X, tile.Y - 1);
        var nullablePointInfo = GetPointInfo(tileAbove);
        Vector2I? startTile = null;

        if (nullablePointInfo is PointInfo pointInfo)
            if (pointInfo.IsLeftEdge)
                startTile = new(tileAbove.X - 1, tileAbove.Y);
            else if (pointInfo.IsRightEdge)
                startTile = new(tileAbove.X + 1, tileAbove.Y);

        return startTile;
    }

    Vector2I? FindFallPoint(Vector2I tile)
    {
        var nullableStartTile = GetStartTileForFallPointScanning(tile);
        // find fall point by scanning downward until it find not empty cell
        if (nullableStartTile is Vector2I startTile)
        {
            for (int y = 0; y < MAX_TILE_FALL_SCAN_DEPTH; ++y)
            {
                Vector2I fallTile = new(startTile.X, startTile.Y + y);
                if (GetCellSourceId(COLLISION_LAYER, fallTile) != EMPTY_CELL)
                    return new(fallTile.X, fallTile.Y - 1);
            }
        }
        return null;
    }

    private void AddGrapthPoints()
    {
        foreach (var tile in _usedTiles)
        {
            AddLeftEdgePoint(tile);
            AddRightEdgePoint(tile);
            AddLeftWallPoint(tile);
            AddRightWallPoint(tile);
            AddFallPoint(tile);
        }
    }

    #region TILE FALL GRAPTH POINTS
    void AddFallPoint(Vector2I tile)
    {
        var nullableFallTile = FindFallPoint(tile);
        if (nullableFallTile is Vector2I fallTile)
        {
            var existingPointId = IsTileExistedInGrapth(fallTile);
            var fallTileLocal = (Vector2I)MapToLocal(fallTile);
            // if the point not existed add a point and mark it as a fall tile
            if (existingPointId == -1)
            {
                var id = _aStarGrapth.GetAvailablePointId();
                PointInfo pointInfo = new(id, fallTileLocal) { IsFallTile = true };
                _pointInfoList.Add(pointInfo);
                _aStarGrapth.AddPoint(id, fallTileLocal);
                AddVisualPoint(fallTile, new Color(1, 0.35f, 0.1f, 1), 0.35f); // small orange
            }
            // else mark this as a fall tile
            else
            {
                _pointInfoList.Single((p) => p.PointId == existingPointId).IsFallTile = true;
                AddVisualPoint(fallTile, new Color("#ef7d57"), 0.3f); // even smaller orange
            }
        }
    }
    #endregion

    #region TILE EDGE & WALL GRAPTH POINTS
    void AddLeftEdgePoint(Vector2I tile)
    {
        if (CheckTileAboveExisted(tile))
            return;
        // the tile on the left is x - 1
        // check left tile existed ?. If not this is a left edge tile
        if (GetCellSourceId(COLLISION_LAYER, new Vector2I(tile.X - 1, tile.Y)) == EMPTY_CELL)
        {
            var tileAbove = new Vector2I(tile.X, tile.Y - 1); // the point (tile) to follow
            long existingPointId = IsTileExistedInGrapth(tileAbove);
            // if the point not existed add a point and mark it as a left edge
            if (existingPointId == -1)
            {
                var id = _aStarGrapth.GetAvailablePointId();
                PointInfo pointInfo =
                    new(id, (Vector2I)MapToLocal(tileAbove)) { IsLeftEdge = true };
                _pointInfoList.Add(pointInfo);
                _aStarGrapth.AddPoint(id, (Vector2I)MapToLocal(tileAbove));
                AddVisualPoint(tileAbove);
            }
            // else mark this as a left edge
            // * note: a point can be have multiple property they can be both right wall and left edge. That call a share point. The check underneath is to make a share point.
            else
            {
                _pointInfoList.Single((p) => p.PointId == existingPointId).IsLeftEdge = true;
                AddVisualPoint(tileAbove, new Color("#73eff7"));
            }
        }
    }

    void AddRightEdgePoint(Vector2I tile)
    {
        if (CheckTileAboveExisted(tile))
            return;
        // the tile on the right is x + 1
        // check right tile existed ?. If not this is a right edge tile
        if (GetCellSourceId(COLLISION_LAYER, new Vector2I(tile.X + 1, tile.Y)) == EMPTY_CELL)
        {
            var tileAbove = new Vector2I(tile.X, tile.Y - 1); // the point (tile) to follow
            long existingPointId = IsTileExistedInGrapth(tileAbove);
            // if the point not existed add a point and mark it as a right edge
            if (existingPointId == -1)
            {
                var id = _aStarGrapth.GetAvailablePointId();
                PointInfo pointInfo =
                    new(id, (Vector2I)MapToLocal(tileAbove)) { IsRightEdge = true };
                _pointInfoList.Add(pointInfo);
                _aStarGrapth.AddPoint(id, (Vector2I)MapToLocal(tileAbove));
                AddVisualPoint(tileAbove, new Color("#94b0c2"));
            }
            // else mark this as a right edge share point
            else
            {
                _pointInfoList.Single((p) => p.PointId == existingPointId).IsRightEdge = true;
                AddVisualPoint(tileAbove, new Color("#ffcd75"), 0.25f);
            }
        }
    }

    void AddLeftWallPoint(Vector2I tile)
    {
        if (CheckTileAboveExisted(tile))
            return;
        // we look up and look to the left to find the left wall tile (x - 1, y - 1)
        // if it the tile exist it is a left wall
        if (GetCellSourceId(COLLISION_LAYER, new Vector2I(tile.X - 1, tile.Y - 1)) != EMPTY_CELL)
        {
            var tileAbove = new Vector2I(tile.X, tile.Y - 1); // the point (tile) to follow
            long existingPointId = IsTileExistedInGrapth(tileAbove);
            // if the point not existed add a point and mark it as a left wall
            if (existingPointId == -1)
            {
                var id = _aStarGrapth.GetAvailablePointId();
                PointInfo pointInfo =
                    new(id, (Vector2I)MapToLocal(tileAbove)) { IsLeftWall = true };
                _pointInfoList.Add(pointInfo);
                _aStarGrapth.AddPoint(id, (Vector2I)MapToLocal(tileAbove));
                AddVisualPoint(tileAbove, new Color("pink")); // red
            }
            // else mark this as a left wall share point
            else
            {
                _pointInfoList.Single((p) => p.PointId == existingPointId).IsLeftWall = true;
                AddVisualPoint(tileAbove, new Color(0, 0, 1, 1), 0.2f); // small blue
            }
        }
    }

    void useDebugFunction(Vector2I tile)
    {
        AddVisualPoint(tile, new("red"));
    }

    void AddRightWallPoint(Vector2I tile)
    {
        if (CheckTileAboveExisted(tile))
            return;
        // we look up and to the left to find the right wall tile (x + 1, y - 1)
        // if it the tile exist it is a right wall
        if (GetCellSourceId(COLLISION_LAYER, new Vector2I(tile.X + 1, tile.Y - 1)) != EMPTY_CELL)
        {
            var tileAbove = new Vector2I(tile.X, tile.Y - 1); // the point (tile) to follow
            long existingPointId = IsTileExistedInGrapth(tileAbove);
            // if the point not existed add a point and mark it as a right wall
            if (existingPointId == -1)
            {
                var id = _aStarGrapth.GetAvailablePointId();
                PointInfo pointInfo =
                    new(id, (Vector2I)MapToLocal(tileAbove)) { IsRightWall = true };
                _pointInfoList.Add(pointInfo);
                _aStarGrapth.AddPoint(id, (Vector2I)MapToLocal(tileAbove));
                AddVisualPoint(tileAbove, new Color("pink")); // red
            }
            // else mark this as a right wall share point
            else
            {
                _pointInfoList.Single((p) => p.PointId == existingPointId).IsRightWall = true;
                AddVisualPoint(tileAbove, new Color("#566c86"), 0.65f); // small purple blue
            }
        }
    }

    void AddVisualPoint(Vector2I tileAbove, Color? maybeColor = null, float scale = 1.0f)
    {
        if (!ShowDebugGrapth)
            return;

        Sprite2D visualPoint = _grapthPoint.Instantiate<Sprite2D>();

        if (maybeColor is Color color)
        {
            visualPoint.Modulate = color;
        }

        if (scale != 1.0f && scale > 0.1f)
            visualPoint.Scale = new(scale, scale);
        visualPoint.Position = MapToLocal(tileAbove);
        AddChild(visualPoint);
    }

    long IsTileExistedInGrapth(Vector2I tile)
    {
        var localPos = MapToLocal(tile);

        if (_aStarGrapth.GetPointCount() > 0)
        {
            var id = _aStarGrapth.GetClosestPoint(localPos);
            if (_aStarGrapth.GetPointPosition(id) == localPos)
                return id;
        }

        return -1;
    }

    bool CheckTileAboveExisted(Vector2I tile)
    {
        // the tile above is y - 1
        if (GetCellSourceId(COLLISION_LAYER, new Vector2I(tile.X, tile.Y - 1)) == EMPTY_CELL)
            return false;
        return true;
    }
    #endregion

    #region CONNECT GRAPTH POINTS
    void ConnectPoints()
    {
        // we always connect from left to right
        foreach (var p1 in _pointInfoList)
        {
            ConnectHorizontalPoints(p1);
            ConnectJumpPoints(p1);
            ConnectFallPoint(p1);
        }
    }

    void ConnectHorizontalPoints(PointInfo p1)
    {
        if (p1.IsLeftEdge || p1.IsLeftWall || p1.IsFallTile)
        {
            PointInfo? closestToP1 = null;
            foreach (var p2 in _pointInfoList)
            {
                // check if p2 is a right node (same height, X is farther right)
                if (
                    (p2.IsRightWall || p2.IsRightEdge || p2.IsFallTile)
                    && p2.PointId != p1.PointId
                    && p2.Position.X > p1.Position.X
                    && p2.Position.Y == p1.Position.Y
                )
                {
                    closestToP1 ??= new(p2.PointId, p2.Position);
                    if (p2.Position.X < closestToP1.Position.X)
                        closestToP1 = new(p2.PointId, p2.Position);
                }
            }

            if (closestToP1 != null && CanConnectHorizontally(p1.Position, closestToP1.Position))
            {
                _aStarGrapth.ConnectPoints(p1.PointId, closestToP1.PointId);
                DrawDebugLine(p1.Position, closestToP1.Position, new("green"));
            }
        }
    }

    bool CanConnectHorizontally(Vector2 p1, Vector2 p2)
    {
        var startScanTile = LocalToMap(p1);
        var endScanTile = LocalToMap(p2);

        for (int i = startScanTile.X; i < endScanTile.X; ++i)
        {
            // if to the right (x + 1) is a wall (empty_cell) then we can't connect
            // if below is empty cell we can't connect because we will implement this gap with a jump point connection
            if (
                GetCellSourceId(COLLISION_LAYER, new(i, startScanTile.Y)) != EMPTY_CELL
                || GetCellSourceId(COLLISION_LAYER, new(i, startScanTile.Y + 1)) == EMPTY_CELL
            )
                return false;
        }

        return true;
    }

    void ConnectJumpPoints(PointInfo p1)
    {
        foreach (var p2 in _pointInfoList)
        {
            ConnectHorizontalPlatformJump(p1, p2);
            ConnectJumpFallFromLeftToRightEdge(p1, p2);
            ConnectJumpFallFromRightToLeftEdge(p1, p2);
        }
    }

    void ConnectHorizontalPlatformJump(PointInfo p1, PointInfo p2)
    {
        if (p1.PointId == p2.PointId)
            return;

        // if p1 is a right edge and p2 is a left edge, they sit on the same level
        if (
            p1.Position.Y == p2.Position.Y
            && p1.Position.X < p2.Position.X
            && p1.IsRightEdge
            && p2.IsLeftEdge
        )
        {
            Vector2 p1MapCoor = LocalToMap(p1.Position);
            Vector2 p2MapCoor = LocalToMap(p2.Position);

            // if within jumpDistance
            if (p2MapCoor.DistanceTo(p1MapCoor) < JumpDistance + 1)
            {
                _aStarGrapth.ConnectPoints(p1.PointId, p2.PointId);
                DrawDebugLine(p1.Position, p2.Position, new("yellow"));
            }
        }
    }

    void ConnectJumpFallFromLeftToRightEdge(PointInfo p1, PointInfo p2)
    {
        if (p1.IsLeftEdge && p2.IsRightEdge)
        {
            Vector2 p1MapCoor = LocalToMap(p1.Position);
            Vector2 p2MapCoor = LocalToMap(p2.Position);
            // p1 have to be taller than p2 and
            if (
                p1.Position.X > p2.Position.X
                && p1.Position.Y < p2.Position.Y
                && p2MapCoor.DistanceTo(p1MapCoor) < JumpDistance
            )
            {
                _aStarGrapth.ConnectPoints(p1.PointId, p2.PointId);
                DrawDebugLine(p1.Position, p2.Position, new("yellow"));
            }
        }
    }

    void ConnectJumpFallFromRightToLeftEdge(PointInfo p1, PointInfo p2)
    {
        if (p1.IsRightEdge && p2.IsLeftEdge)
        {
            Vector2 p1MapCoor = LocalToMap(p1.Position);
            Vector2 p2MapCoor = LocalToMap(p2.Position);
            // p1 have to be taller than p2
            if (
                p1.Position.X < p2.Position.X
                && p1.Position.Y < p2.Position.Y
                && p2MapCoor.DistanceTo(p1MapCoor) < JumpDistance
            )
            {
                _aStarGrapth.ConnectPoints(p1.PointId, p2.PointId);
                DrawDebugLine(p1.Position, p2.Position, new("yellow"));
            }
        }
    }

    void ConnectFallPoint(PointInfo p1)
    {
        if (p1.IsLeftEdge || p1.IsRightEdge)
        {
            var p1TilePos = LocalToMap(p1.Position);
            var nullableFallPoint = FindFallPoint(new(p1TilePos.X, p1TilePos.Y + 1)); // this get the exact pos of a tile, all our point have been above it actual tile
            if (nullableFallPoint is Vector2I fallPoint)
            {
                var nullableFallPointInfo = GetPointInfo(fallPoint);
                if (nullableFallPointInfo is PointInfo fallPointInfo && fallPointInfo.IsFallTile)
                {
                    Vector2 fallPointMapPos = LocalToMap(fallPointInfo.Position);
                    if (fallPointMapPos.DistanceTo(p1TilePos) <= JumpHeight)
                    {
                        _aStarGrapth.ConnectPoints(p1.PointId, fallPointInfo.PointId);
                        DrawDebugLine(p1.Position, fallPointInfo.Position, new("yellow"));
                    }
                    else
                    {
                        _aStarGrapth.ConnectPoints(p1.PointId, fallPointInfo.PointId, false);
                        DrawDebugLine(p1.Position, fallPointInfo.Position, new("red"));
                    }
                }
            }
        }
    }
    #endregion
}
