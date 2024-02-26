using Godot;
using Lanboost.PathFinding.Astar;
using Lanboost.PathFinding.Graph;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using static Godot.Time;

public struct NavigationPoint
{
    public float X;
    public float Y;
    public int Layer;

    public NavigationPoint(float x, float y, int layer)
    {
        X = x;
        Y = y;
        Layer = layer;
    }

    public static NavigationPoint NaN = new NavigationPoint(float.NaN, float.NaN, int.MaxValue);
}

public class NavMesh
{

    IBaseCollisionWorld world;
    NavMeshCreator creator;
    NavMeshGraph graph;

    public NavMesh(IBaseCollisionWorld world)
    {
        this.world = world;
        this.creator = new NavMeshCreator(world);
        this.graph = new NavMeshGraph();
    }

    /** This method will create a navmesh chunk, it will not link meshes towards neighbor chunks, 
     * you need to explicity call GenerateAllRemoteLinks / GenerateRemoteLinks.
     * 
     */
    public void GenerateChunk(int chunkX, int chunkY)
    {
        this.graph.UnloadChunk(chunkX, chunkY);
        var rects = this.creator.Create(chunkX, chunkY);
        var edges = this.creator.CreateEdges(rects);
        var chunk = new NavMeshChunk(chunkX, chunkY, rects, edges);
        this.graph.LoadChunk(chunk);
    }

    public void GenerateAllRemoteLinks(int chunkX, int chunkY) {
        foreach (EDirection value in Enum.GetValues(typeof(EDirection)))
        {
            GenerateRemoteLinks(chunkX, chunkY, value);
        }
    }
    public void GenerateRemoteLinks(int chunkX, int chunkY, EDirection direction) {

        var offset = DirectionUtil.Offset(direction);
        var opposite = DirectionUtil.Opposite(direction);

        var fromKey = PositionKey.Key(chunkX, chunkY);
        var toKey = PositionKey.Key(chunkX+ offset.X, chunkY+ offset.Y);

        var from = this.graph.GetChunk(fromKey);
        var to = this.graph.GetChunk(toKey);

        if (from == null || to == null) return;

        this.graph.UnloadChunk(chunkX, chunkY);
        this.graph.UnloadChunk(chunkX + offset.X, chunkY + offset.Y);

        

        (var fromEdges,var toEdges) = this.creator.CreateRemoteEdges(fromKey, toKey, from.navMeshRects, to.navMeshRects);

        from.remoteNavMeshEdges[(int)direction] = fromEdges;
        to.remoteNavMeshEdges[(int)opposite] = toEdges;

        from.remoteEdgeFlags = from.remoteEdgeFlags | (int) DirectionUtil.BitFlag(direction);
        to.remoteEdgeFlags = to.remoteEdgeFlags | (int)DirectionUtil.BitFlag(opposite);

        this.graph.LoadChunk(from);
        this.graph.LoadChunk(to);

    }

    public void ReadChunk(int chunkX, int chunkY) { }
    public void SaveChunk(int chunkX, int chunkY) { }

    public void AddChunk() { }

    public void RemoveChunk(int chunkX, int chunkY) {
        this.graph.UnloadChunk(chunkX,chunkY);
    }

    public NavigationPoint GetClosestMeshPoint(Vector2 point, int layer)
    {
        return NavigationPoint.NaN;
    }

    public List<NavigationPoint> FindPath(NavigationPoint from, NavigationPoint to)
    {
        // Ensure start and end is not in same rect
        var s = this.graph.GetRectFromPosition(new NavMeshRect(from.X, from.Y, 0,0, from.Layer));
        var e = this.graph.GetRectFromPosition(new NavMeshRect(to.X, to.Y, 0, 0, to.Layer));

        if (s == null || e == null)
        {
            return null;
        }
        else
        {
            if (s == e)
            {
                List<NavigationPoint> path = new List<NavigationPoint>();
                path.Add(from);
                path.Add(to);
                return path;
            }
        }


        var gpath = this.graph.FindPath(from, to);
        if(gpath != null)
        {
            /* If you need to debug edge path...
            List<NavigationPoint> path = new List<NavigationPoint>();
            path.Add(from);
            foreach(var p in gpath)
            {
                var mid = (p.right - p.left) / 2 + p.left;
                path.Add(new NavigationPoint(mid.X, mid.Y, 0));
            }
            path.Add(to);
            return path;
            */




            List<NavigationPoint> path = new List<NavigationPoint>();
            path.Add(from);
            List<SSFState> states = null;
            foreach(var p in SimpleStupidFunnel.Run(new Vector2(from.X, from.Y), new Vector2(to.X, to.Y), gpath, ref states))
            {
                path.Add(new NavigationPoint(p.X, p.Y, 0));
            }
            path.Add(to);
            return path;
        }
        return null;
    }

    public IEnumerable<NavMeshChunk> AllChunks()
    {
        foreach (var chunk in graph.AllChunks())
        {
            yield return chunk;
        }
    }

    public NavMeshChunk GetChunk(int chunkX, int chunkY)
    {
        var chunkKey = PositionKey.Key(chunkX, chunkY);
        return graph.GetChunk(chunkKey);
    }

    public int ChunkSize()
    {
        return world.ChunkSize();
    }
}


public class NavMeshEdge
{
    public NavMeshRect to;

    // Left / right here, is determined by which direction the edge is going
    // relative to the triangulation in Simple Stupid Funnel algorithm
    public Vector2 left;
    public Vector2 right;
    public float cost;
    //used for remote edges
    public ulong chunkKey = ulong.MaxValue;
    public int toId;

    public NavMeshEdge(NavMeshRect to, Vector2 left, Vector2 right, float cost)
    {
        this.to = to;
        this.left = left;
        this.right = right;
        this.cost = cost;
    }
}

public class NavMeshRect
{
    public float sx;
    public float sy;
    public float width;
    public float height;
    public int layer = 0;
    // id is only for linking chunks together
    public int id;

    public List<NavMeshEdge> edges = new List<NavMeshEdge>();

    public NavMeshRect(float sx, float sy, float width, float height, int layer, int id = 0)
    {
        this.sx = sx;
        this.sy = sy;
        this.width = width;
        this.height = height;
        this.layer = layer;
        this.id = id;
    }

    public float ManhattanDistance(NavMeshRect other)
    {
        var midx1 = this.sx + this.width / 2;
        var midy1 = this.sy + this.height / 2;

        var midx2 = other.sx + other.width / 2;
        var midy2 = other.sy + other.height / 2;

        return Math.Abs(midx1 - midx2) + Math.Abs(midy1 - midy2);
    }

    public NavMeshEdge CreateEdge(NavMeshRect other)
    {
        var delta = 0.001f;

        var deltaBoxSx = other.sx - delta;
        var deltaBoxSy = other.sy - delta;
        var deltaBoxWidth = other.width + 2 * delta;
        var deltaBoxHeight = other.height + 2 * delta;

        // check box intersect
        if (this.sx <= deltaBoxSx + deltaBoxWidth && deltaBoxSx <= this.sx + this.width)
        {
            if (this.sy <= deltaBoxSy + deltaBoxHeight && deltaBoxSy <= this.sy + this.height)
            {
                // With inaccuracies, we can intersect diagonally, this is not really intended, check that the area of the intersecting box
                // is large enough

                var width = Math.Min(this.sx+this.width, deltaBoxSx+deltaBoxWidth)-Math.Max(this.sx, deltaBoxSx);
                var height = Math.Min(this.sy + this.height, deltaBoxSy + deltaBoxHeight) - Math.Max(this.sy, deltaBoxSy);

                if(width < 2*delta && height < 2*delta)
                {
                    return null;
                }


                Vector2 left, right;

                int sx, sy, ex, ey;
                // one side should line up with other
                // this is to the right going <-
                if (Math.Abs(this.sx- (other.sx + other.width)) < delta)
                {
                    var ytop = Math.Max(this.sy, other.sy);
                    var ybottom = Math.Min(this.sy + this.height, other.sy + other.height);
                    left = new Vector2(this.sx, ybottom);
                    right = new Vector2(this.sx, ytop);
                }
                // left ->
                else if (Math.Abs((this.sx + this.width) - other.sx) < delta)
                {
                    var ytop = Math.Max(this.sy, other.sy);
                    var ybottom = Math.Min(this.sy + this.height, other.sy + other.height);
                    left = new Vector2(other.sx, ytop);
                    right = new Vector2(other.sx, ybottom);
                }
                // bottom ^
                else if (Math.Abs(this.sy - (other.sy + other.height)) < delta)
                {
                    var xleft = Math.Max(this.sx, other.sx);
                    var xright = Math.Min(this.sx + this.width, other.sx + other.width);
                    left = new Vector2(xleft, this.sy);
                    right = new Vector2(xright, this.sy);

                }
                // top v
                else if (Math.Abs((this.sy + this.height) - other.sy) < delta)
                {
                    var xleft = Math.Max(this.sx, other.sx);
                    var xright = Math.Min(this.sx + this.width, other.sx + other.width);
                    left = new Vector2(xright, other.sy);
                    right = new Vector2(xleft, other.sy);

                }
                else
                {
                    throw new Exception("Wtf? Real overlap?");
                }

                return new NavMeshEdge(other, left, right, ManhattanDistance(other));
            }
        }
        return null;
    }
}

public enum EDirection
{
    North = 0,
    NorthEast = 1,
    East = 2,
    SouthEast = 3,
    South = 4,
    SouthWest = 5,
    West = 6,
    NorthWest = 7
}

public class DirectionUtil
{
    protected static EDirection[] opposite = new EDirection[]
    {
        EDirection.South,
        EDirection.SouthWest,
        EDirection.West,
        EDirection.NorthWest,
        EDirection.North,
        EDirection.NorthEast,
        EDirection.East,
        EDirection.SouthEast
    };

    protected static EDirectionBitFlag[] bitFlag = new EDirectionBitFlag[]
    {
        EDirectionBitFlag.North,
        EDirectionBitFlag.NorthEast,
        EDirectionBitFlag.East,
        EDirectionBitFlag.SouthEast,
        EDirectionBitFlag.South,
        EDirectionBitFlag.SouthWest,
        EDirectionBitFlag.West,
        EDirectionBitFlag.NorthWest,
    };

    protected static Vector2I[] offset = new Vector2I[]
    {
        new Vector2I(0, 1),
        new Vector2I(1, 1),
        new Vector2I(1, 0),
        new Vector2I(1, -1),
        new Vector2I(0, -1),
        new Vector2I(-1, -1),
        new Vector2I(-1, 0),
        new Vector2I(-1, 1),
    };

    public static Vector2I Offset(EDirection dir)
    {
        return offset[(int)dir];
    }

    public static EDirection Opposite(EDirection dir)
    {
        return (EDirection)opposite[(int)dir];
    }

    public static EDirectionBitFlag BitFlag(EDirection dir)
    {
        return (EDirectionBitFlag)bitFlag[(int)dir];
    }
}

public enum EDirectionBitFlag
{
    None = 0,
    North = 1,
    NorthEast = 2,
    East = 4,
    SouthEast = 8,
    South = 16,
    SouthWest = 32,
    West = 64,
    NorthWest = 128,
    All = 255
}

public class NavMeshChunk
{
    public List<NavMeshRect> navMeshRects = new List<NavMeshRect>();
    public List<NavMeshEdge> navMeshEdges;
    public List<NavMeshEdge>[] remoteNavMeshEdges;
    public int x;
    public int y;

    // Use ERemoteEdgeGenerated, this is used to indicate if we have "recreated" the chunk, so outside chunks
    // will not be able to link, if we are not "linked" to it.
    public int remoteEdgeFlags = 0;

    public NavMeshChunk(int x, int y, List<NavMeshRect> navMeshRects, List<NavMeshEdge> navMeshEdges)
    {
        this.navMeshRects = navMeshRects;
        this.x = x;
        this.y = y;
        this.navMeshEdges = navMeshEdges;
        remoteNavMeshEdges = new List<NavMeshEdge>[8];
    }
}

public class PositionKey
{
    static int BitsForY = 32;
    public static ulong Key(int x, int y)
    {
        x++;
        y++;

        if(x <= 0 || y <= 0)
        {
            return ulong.MaxValue;
        }
        var ulongy = (ulong)y;
        var ulongx = (ulong)x;
        return (ulongy << BitsForY) + ulongx;
    }

    public static (int, int) Explode(ulong key)
    {
        ulong bitflag = 0;
        unchecked
        {
            bitflag = (((ulong)1 << BitsForY) - 1);
        }

        int x = (int) (key & bitflag);
        int y = (int) ((key >> BitsForY) & bitflag);
        x--;
        y--;
        return (x, y);
    }
}

public class NavMeshGraph : IGraph<NavMeshRect, NavMeshEdge>
{
    AStar<NavMeshRect, NavMeshEdge> astar;

    public Dictionary<ulong, NavMeshChunk> chunks = new Dictionary<ulong, NavMeshChunk>();


    public Dictionary<NavMeshRect, NavMeshEdge> extraEndEdges = new Dictionary<NavMeshRect, NavMeshEdge>();
    public int chunkSize = 64;
    public NavMeshGraph()
    {
        astar = new AStar<NavMeshRect, NavMeshEdge>(this, 100);
    }

    public IEnumerable<NavMeshChunk> AllChunks()
    {
        foreach(var chunk in chunks.Values)
        {
            yield return chunk;
        }
    }

    public NavMeshChunk GetChunk(ulong key)
    {
        if(chunks.ContainsKey(key))
        {
            return chunks[key];
        }
        return null;
    }

    void ConnectChunkLinks(NavMeshChunk from, NavMeshChunk to, ulong toKey)
    {
        foreach(var rects in from.navMeshRects)
        {
            foreach(var edge in rects.edges)
            {
                if(edge.chunkKey == toKey)
                {
                    edge.to = to.navMeshRects[edge.toId];
                }
            }
        }
    }

    void DisconnectChunkLinks(NavMeshChunk from, ulong toKey)
    {
        foreach (var rects in from.navMeshRects)
        {
            foreach (var edge in rects.edges)
            {
                if (edge.chunkKey == toKey)
                {
                    edge.to = null;
                }
            }
        }
    }

    public void LoadChunk(NavMeshChunk chunk)
    {
        var chunkKey = PositionKey.Key(chunk.x, chunk.y);
        this.chunks.Add(chunkKey, chunk);
        //iterate over surrounding chunks, and add remote edges if 

        for(int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if(x == 0 && y==0)
                {
                    continue;
                }

                var key = PositionKey.Key(chunk.x - x, chunk.y - y);
                if(chunks.ContainsKey(key))
                {
                    var other = chunks[key];
                    ConnectChunkLinks(chunk, other, key);
                    ConnectChunkLinks(other, chunk, chunkKey);
                }
            }
        }

    }

    public void UnloadChunk(int chunkX, int chunkY)
    {
        var chunkKey = PositionKey.Key(chunkX, chunkY);
        if (chunks.ContainsKey(chunkKey))
        {
            this.chunks.Remove(chunkKey);
            //iterate over surrounding chunks, and remove remote edges if needed

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    var key = PositionKey.Key(chunkX - x, chunkY - y);
                    if (chunks.ContainsKey(key))
                    {
                        var other = chunks[key];
                        DisconnectChunkLinks(other, chunkKey);
                    }
                }
            }
        }
    }

    public NavMeshRect GetRectFromPosition(NavMeshRect position)
    {
        var key = PositionKey.Key((int)(position.sx/ chunkSize), (int)(position.sy/ chunkSize));

        if(chunks.ContainsKey(key))
        {
            foreach (var rect in chunks[key].navMeshRects)
            {
                if (rect.sx <= position.sx && position.sx < rect.sx + rect.width)
                {
                    if (rect.sy <= position.sy && position.sy < rect.sy + rect.height)
                    {
                        if(rect.layer == position.layer)
                        {
                            return rect;
                        }
                    }
                }
            }
        }
        return null;
    }

    public void AddTemporaryStartEndNodes(NavMeshRect start, NavMeshRect end)
    {
        var s = GetRectFromPosition(start);
        var e = GetRectFromPosition(end);

        if (s == null || e == null)
        {
            //return null;
        }
        else
        {
            if (s == e)
            {
                //start.edges.Add(new NavMeshEdge(end, se.left, se.right, 0));
                throw new Exception("Shoudl ensure this doesnt happen...Lan...");
            }
            else
            {
                



                foreach (var se in s.edges)
                {
                    var cost = s.ManhattanDistance(se.to);
                    start.edges.Add(new NavMeshEdge(se.to, se.left, se.right, cost));
                    // check if start and end is neightbor, if so create a direct link...
                    if(se.to == e)
                    {
                        start.edges.Add(new NavMeshEdge(end, se.left, se.right, cost));
                    }

                }

                extraEndEdges.Clear();
                foreach (var ee in e.edges)
                {
                    var cost = e.ManhattanDistance(ee.to);
                    extraEndEdges.Add(ee.to, new NavMeshEdge(end, ee.right, ee.left, cost));
                }
            }
        }
    }

    public int GetCost(NavMeshRect from, NavMeshRect to, NavMeshEdge link)
    {
        return (int)link.cost;
    }

    public IEnumerable<Edge<NavMeshRect, NavMeshEdge>> GetEdges(NavMeshRect node)
    {
        foreach (var edge in node.edges)
        {
            if(edge.to == null)
            {
                continue;
            }
            //var e = node != edge.left ? edge.left : edge.right;
            yield return new Edge<NavMeshRect, NavMeshEdge>(edge.to, edge);
        }
        if (extraEndEdges.ContainsKey(node))
        {
            var extra = extraEndEdges[node];
            if (extra.to != null)
            {
                yield return new Edge<NavMeshRect, NavMeshEdge>(extra.to, extra);
            }
        }
    }

    public int GetEstimation(NavMeshRect from, NavMeshRect to)
    {
        return (int)from.ManhattanDistance(to);
    }

    internal List<NavMeshEdge> FindPath(NavigationPoint start, NavigationPoint end)
    {
        var startRect = new NavMeshRect(start.X, start.Y, 0, 0, start.Layer);
        startRect.layer = start.Layer;

        var endRect = new NavMeshRect(end.X, end.Y, 0, 0, end.Layer);
        endRect.layer = end.Layer;

        var failure = this.astar.FindPath(startRect, endRect);
        if (failure == null)
        {
            return this.astar.GetPathLinks();
        }
        else
        {
            GD.Print(failure);
        }
        return null;
    }
}
