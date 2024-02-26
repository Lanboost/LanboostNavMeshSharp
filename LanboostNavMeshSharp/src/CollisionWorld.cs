using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum ENineCollisionFlag
{
    None = 0,
    Full = 511,

    Middle = 1,
    North = 2,
    NorthEast = 4,
    East = 8,
    SouthEast = 16,
    South = 32,
    SouthWest = 64,
    West = 128,
    NorthWest = 256
}

public interface ICollisionWorld<T>
{
    public int ChunkSize();

    public float TileScale();
    public IEnumerable<T> GetChunk(int x, int y);

    public IEnumerable<ulong> GetAvailableChunks();
}

public interface INineCollisionWorld : ICollisionWorld<int>
{
}

public interface IBaseCollisionWorld : ICollisionWorld<bool>
{
}

public class SubdivideCollisionWorld : IBaseCollisionWorld
{
    bool[] rowFlags;
    IBaseCollisionWorld world;
    int divisions;

    public SubdivideCollisionWorld(IBaseCollisionWorld world, int divisions)
    {
        this.world = world;
        this.divisions = divisions;
        rowFlags = new bool[world.ChunkSize()];
    }

    public int ChunkSize()
    {
        return world.ChunkSize() * divisions;
    }

    IEnumerable<bool> ExpandTileFlagRowToTileRows(bool[] flags)
    {
        for (int row = 0; row < 3; row++)
        {
            for (int ix = 0; ix < flags.Length; ix++)
            {
                for (int i = 0; i < 3; i++)
                {
                    yield return flags[ix];
                }
            }
        }
    }


    public IEnumerable<bool> GetChunk(int x, int y)
    {
        int cx = 0;
        int cy = 0;
        // We need to expand each cell into a 3x3, so store an entire row, then output 3 rows back with 3 times the cells
        foreach (var blocked in world.GetChunk(x, y))
        {
            rowFlags[cx] = blocked;
            cx++;
            if (cx == world.ChunkSize())
            {
                foreach (var temp in ExpandTileFlagRowToTileRows(rowFlags))
                {
                    yield return temp;
                }

                cx = 0;
                cy++;
            }
        }
    }

    public IEnumerable<ulong> GetAvailableChunks()
    {
        foreach (var key in world.GetAvailableChunks())
        {
            yield return key;
        }
    }

    public float TileScale()
    {
        return world.TileScale() * 1 / 3f;
    }
}


public class ExpandedTileCollisionWorld : IBaseCollisionWorld
{
    IBaseCollisionWorld world;
    bool[][] tempChunk;
    bool[] tempRow;

    public ExpandedTileCollisionWorld(IBaseCollisionWorld world)
    {
        this.world = world;

        // Create temp arrays of size 2 bigger (to be able to store surrounding chunk block data)
        tempChunk = new bool[world.ChunkSize() + 2][];
        tempRow = new bool[world.ChunkSize() + 2];
        for (int iy = 0; iy < tempChunk.Length; iy++)
        {
            tempChunk[iy] = new bool[world.ChunkSize() + 2];
        }
    }

    public int ChunkSize()
    {
        return world.ChunkSize();
    }

    void LoadMainChunkIntoTemp(int x, int y)
    {
        int cx = 0;
        int cy = 0;
        // Copy over from underlying world, with 1,1 in offset (so we can get blocked from surrounding chunks)
        foreach (var blocked in world.GetChunk(x, y))
        {
            tempChunk[cy + 1][cx + 1] = blocked;
            cx++;
            if (cx == this.ChunkSize())
            {
                cx = 0;
                cy++;
            }
        }
    }

    void LoadSurroundingChunksIntoTemp(int x, int y)
    {

        // Copy over surrounding chunks from underlying world
        for (int iy = -1; iy <= 1; iy++)
        {
            for (int ix = -1; ix <= 1; ix++)
            {
                if (ix == 0 && iy == 0)
                {
                    continue;
                }

                int cx = 0;
                int cy = 0;
                foreach (var blocked in world.GetChunk(x + ix, y + iy))
                {
                    int nx = -1;
                    int ny = -1;
                    if (ix == -1)
                    {
                        if (cx == this.ChunkSize() - 1)
                        {
                            nx = 0;
                        }
                    }
                    else if (ix == 1)
                    {
                        if (cx == 0)
                        {
                            nx = this.ChunkSize() + 1;
                        }
                    }
                    else
                    {
                        nx = cx + 1;
                    }

                    if (iy == -1)
                    {
                        if (cy == this.ChunkSize() - 1)
                        {
                            ny = 0;
                        }
                    }
                    else if (iy == 1)
                    {
                        if (cy == 0)
                        {
                            ny = this.ChunkSize() + 1;
                        }
                    }
                    else
                    {
                        ny = cy + 1;
                    }
                    if (nx >= 0 && ny >= 0)
                    {
                        tempChunk[ny][nx] = blocked;
                    }

                    cx++;
                    if (cx == this.ChunkSize())
                    {
                        cx = 0;
                        cy++;
                    }
                }

            }
        }
    }


    public IEnumerable<bool> GetChunk(int x, int y)
    {
        LoadMainChunkIntoTemp(x, y);
        LoadSurroundingChunksIntoTemp(x, y);

        var size = tempChunk.Length;

        // Expand in X
        for (int outer = 0; outer < size; outer++)
        {
            // First copy over row
            for (int inner = 0; inner < size; inner++)
            {
                tempRow[inner] = tempChunk[outer][inner];
            }

            for (int inner = 0; inner < size; inner++)
            {
                var blocked = false;
                for (int i = -1; i <= 1; i++)
                {
                    var ix = inner + i;
                    if (ix >= 0 && ix < size)
                    {
                        blocked = blocked || tempRow[ix];
                    }
                }
                tempChunk[outer][inner] = blocked;
            }
        }

        // Expand in y (just swapped sx, sy in array
        for (int outer = 0; outer < size; outer++)
        {
            // First copy over row
            for (int inner = 0; inner < size; inner++)
            {
                tempRow[inner] = tempChunk[inner][outer];
            }

            for (int inner = 0; inner < size; inner++)
            {
                var blocked = false;
                for (int i = -1; i <= 1; i++)
                {
                    var ix = inner + i;
                    if (ix >= 0 && ix < size)
                    {
                        blocked = blocked || tempRow[ix];
                    }
                }
                tempChunk[inner][outer] = blocked;
            }
        }

        // Return "main" chunk
        for (int iy = 0; iy < this.ChunkSize(); iy++)
        {
            for (int ix = 0; ix < this.ChunkSize(); ix++)
            {
                yield return tempChunk[iy + 1][ix + 1];
            }
        }
    }

    public IEnumerable<ulong> GetAvailableChunks()
    {
        foreach (var key in world.GetAvailableChunks())
        {
            yield return key;
        }
    }

    public float TileScale()
    {
        return world.TileScale();
    }
}

public class NineBlockCollisionWorld : IBaseCollisionWorld
{
    INineCollisionWorld world;
    int[] rowFlags;

    static int[][] blockLookup = new int[][]
    {
        new int[]
        {
            (int) ENineCollisionFlag.NorthWest,
            (int) ENineCollisionFlag.North,
            (int) ENineCollisionFlag.NorthEast
        },
        new int[]
        {
            (int) ENineCollisionFlag.West,
            (int) ENineCollisionFlag.Middle,
            (int) ENineCollisionFlag.East
        },
        new int[]
        {
            (int) ENineCollisionFlag.SouthWest,
            (int) ENineCollisionFlag.South,
            (int) ENineCollisionFlag.SouthEast
        }
    };

    public NineBlockCollisionWorld(INineCollisionWorld world)
    {
        this.world = world;
        rowFlags = new int[world.ChunkSize()];
    }

    public int ChunkSize()
    {
        return world.ChunkSize() * 3;
    }


    IEnumerable<bool> TileToBlockedRow(int flag, int row)
    {
        for (int i = 0; i < 3; i++)
        {
            yield return (flag & blockLookup[row][i]) > 0;
        }
    }

    IEnumerable<bool> ExpandTileFlagRowToTileRows(int[] flags)
    {
        for (int row = 0; row < 3; row++)
        {
            for (int ix = 0; ix < flags.Length; ix++)
            {
                foreach (var blocked in TileToBlockedRow(flags[ix], row))
                {
                    yield return blocked;
                }
            }
        }
    }


    public IEnumerable<bool> GetChunk(int x, int y)
    {
        int cx = 0;
        int cy = 0;
        // We need to expand each cell into a 3x3, so store an entire row, then output 3 rows back with 3 times the cells
        foreach (var blockFlag in world.GetChunk(x, y))
        {
            rowFlags[cx] = blockFlag;
            cx++;
            if (cx == world.ChunkSize())
            {
                foreach (var blocked in ExpandTileFlagRowToTileRows(rowFlags))
                {
                    yield return blocked;
                }

                cx = 0;
                cy++;
            }
        }
    }

    public IEnumerable<ulong> GetAvailableChunks()
    {
        foreach (var key in world.GetAvailableChunks())
        {
            yield return key;
        }
    }

    public float TileScale()
    {
        return world.TileScale() * 1 / 3f;
    }
}