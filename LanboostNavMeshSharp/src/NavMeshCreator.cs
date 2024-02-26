using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;

public class NavMeshCreator
{
    class Rect
    {
        public int sx;
        public int sy;
        public int width;
        public int height;
        public Rect(int sx, int sy, int width, int height)
        {
            this.sx = sx;
            this.sy = sy;
            this.width = width;
            this.height = height;
        }
    }

    IBaseCollisionWorld world;
    bool[][] used;

    public NavMeshCreator(IBaseCollisionWorld world)
    {
        this.world = world;
        var size = world.ChunkSize();
        used = new bool[size][];
        for (int y = 0; y < used.Length; y++)
        {
            used[y] = new bool[size];
        }
    }




    /*   NavMeshRect ExpandRectGreedy(bool[][] usedCurrent, int sx, int sy, int size)
       {
           int ex = sx;
           int ey = sy;
           while (ex < size - 1)
           {
               if (!usedCurrent[sy][ex + 1])
               {
                   ex++;
                   usedCurrent[sy][ex] = true;
               }
               else
               {
                   break;
               }
           }

           while (ey < size - 1)
           {
               var ok = true;
               for (int x = sx; x <= ex; x++)
               {
                   if (usedCurrent[ey + 1][x])
                   {
                       ok = false;
                       break;
                   }
               }

               if (ok)
               {
                   ey++;
                   for (int x = sx; x <= ex; x++)
                   {
                       usedCurrent[ey][x] = true;
                   }
               }
               else
               {
                   break;
               }
           }
           return new NavMeshRect(sx, sy, ex - sx + 1, ey - sy + 1);
       }

       public List<NavMeshRect> CreateGreedy(IChunk chunk)
       {
           List<NavMeshRect> rects = new List<NavMeshRect>();
           var size = chunk.Size();

           bool[][] used = new bool[size][];
           for (int y = 0; y < used.Length; y++)
           {
               used[y] = new bool[size];
               for (int x = 0; x < used[y].Length; x++)
               {
                   used[y][x] = chunk.Blocked(x, y);
               }
           }

           for (int y = 0; y < used.Length; y++)
           {
               for (int x = 0; x < used[y].Length; x++)
               {
                   if (!used[y][x])
                   {
                       var rect = ExpandRectGreedy(used, x, y, size);
                       rects.Add(rect);
                   }
               }
           }
           return rects;
       }
    */
    bool ExpandRect(bool[][] used, Rect rect, bool horizontal)
    {
        if (horizontal)
        {
            if (used[rect.sy].Length <= rect.sx + rect.width)
            {
                return false;
            }

            for (int y = rect.sy; y < rect.sy + rect.height; y++)
            {

                if (used[y][rect.sx + rect.width])
                {
                    return false;
                }
            }
        }
        else
        {
            if (used.Length <= rect.sy + rect.height)
            {
                return false;
            }

            for (int x = rect.sx; x < rect.sx + rect.width; x++)
            {
                if (used[rect.sy + rect.height][x])
                {
                    return false;
                }
            }
        }
        return true;
    }

    // Max diff and max size is important. Having larger numbers, reduces the complexity of the mesh.
    // however it also introduces the potential for suboptimal paths.
    // having the small values will also force the mesh to become fine and will end up restricting the funnel.
    public List<NavMeshRect> Create(int cx, int cy, int maxDiff = 2, int maxSize = 10)
    {
        List<NavMeshRect> rects = new List<NavMeshRect>();
        var size = world.ChunkSize();


        int ccx = 0;
        int ccy = 0;
        // Copy over from underlying world, with 1,1 in offset (so we can get blocked from surrounding chunks)
        foreach (var blocked in world.GetChunk(cx, cy))
        {
            used[ccy][ccx] = blocked;
            ccx++;
            if (ccx == size)
            {
                ccx = 0;
                ccy++;
            }
        }
        var scale = world.TileScale();
        var chunkX = cx * world.ChunkSize()*scale;
        var chunkY = cy * world.ChunkSize() * scale;

        Rect current = new Rect(0, 0, 0, 0);
        while (true)
        {
            int bestScore = -1;
            Rect best = new Rect(0, 0, 0, 0);

            for (int y = 0; y < used.Length; y++)
            {
                for (int x = 0; x < used[y].Length; x++)
                {
                    var blockedLeft = x == 0 || used[y][x - 1];

                    if (!used[y][x] && blockedLeft)
                    {
                        current.sx = x;
                        current.sy = y;
                        current.width = 1;
                        current.height = 1;

                        // Expand in x
                        while (true)
                        {
                            // Expand in y
                            while (true)
                            {
                                if (current.height >= maxSize || current.height - maxDiff > current.width || !ExpandRect(used, current, false))
                                {
                                    var score = current.width * current.height;
                                    if (score > bestScore)
                                    {
                                        bestScore = score;
                                        best.sx = current.sx;
                                        best.sy = current.sy;
                                        best.width = current.width;
                                        best.height = current.height;
                                    }
                                    break;
                                }
                                else
                                {
                                    current.height += 1;
                                }
                            }
                            // check if we failed before expanding to atleast diffMax
                            if (current.height >= maxSize  || current.width >= maxSize || current.width - maxDiff > current.height)
                            {
                                break;
                            }
                            else
                            {
                                current.height = 1;
                                if (!ExpandRect(used, current, true))
                                {
                                    break;
                                }
                                else
                                {
                                    current.width += 1;
                                }
                            }
                        }
                    }
                }
            }

            if (bestScore > 0)
            {
                //TODO rects.Add(best);
                rects.Add(new NavMeshRect(best.sx* scale+ chunkX, best.sy* scale+ chunkY, best.width * scale, best.height * scale, 0));

                for (int y = best.sy; y < best.sy + best.height; y++)
                {
                    for (int x = best.sx; x < best.sx + best.width; x++)
                    {
                        used[y][x] = true;
                    }
                }
            }
            else
            {
                break;
            }
        }

        return rects;
    }

    public List<NavMeshEdge> CreateEdges(List<NavMeshRect> rects)
    {
        List<NavMeshEdge> edges = new List<NavMeshEdge>();
        for (int curr = 0; curr < rects.Count; curr++)
        {
            for (int next = curr + 1; next < rects.Count; next++)
            {
                // Check overlap / create edge
                var edge = rects[curr].CreateEdge(rects[next]);
                if (edge != null)
                {
                    rects[curr].edges.Add(edge);
                    edges.Add(edge);
                    var second = rects[next].CreateEdge(rects[curr]);
                    edges.Add(second);
                    rects[next].edges.Add(second);
                }
            }
        }
        return edges;
    }

    public (List<NavMeshEdge>, List<NavMeshEdge>) CreateRemoteEdges(ulong fromKey, ulong toKey, List<NavMeshRect> from, List<NavMeshRect> to)
    {
        List<NavMeshEdge> fromEdges = new List<NavMeshEdge>();
        List<NavMeshEdge> toEdges = new List<NavMeshEdge>();
        for (int fi = 0; fi <from.Count; fi++) {
            var fromRect  = from[fi];
            for (int ti = 0; ti < to.Count; ti++)
            {
                var toRect = to[ti];

                var edge = fromRect.CreateEdge(toRect);
                if (edge != null)
                {
                    edge.chunkKey = toKey;
                    edge.toId = ti;

                    fromRect.edges.Add(edge);
                    fromEdges.Add(edge);

                    var second = toRect.CreateEdge(fromRect);
                    second.chunkKey = fromKey;
                    second.toId = fi;

                    toEdges.Add(second);
                    toRect.edges.Add(second);
                }
            }
        }
        return (fromEdges, toEdges);
    }
}
