using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class CollisionWorldExtensions
{
    public static IEnumerable<(int, int, T)> GetChunkSimple<T>(this ICollisionWorld<T> world, int x, int y)
    {
        int cx = 0;
        int cy = 0;
        // Copy over from underlying world, with 1,1 in offset (so we can get blocked from surrounding chunks)
        foreach (var value in world.GetChunk(x, y))
        {
            yield return (cx, cy, value);
            cx++;
            if (cx == world.ChunkSize())
            {
                cx = 0;
                cy++;
            }
        }
    }
}