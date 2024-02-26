using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class WaveFunctionCollapseWorld : IBaseCollisionWorld
{
    int chunkSize = 8;
    public Dictionary<ulong, bool[][]> chunks = new Dictionary<ulong, bool[][]>();

    public WaveFunctionCollapseWorld(int chunkSize)
    {
        this.chunkSize = chunkSize;
    }

    public void GenerateChunk(int x, int y)
    {
        ITopoArray<char> sample = TopoArray.Create(new[]
        {
            //https://github.com/mxgmn/WaveFunctionCollapse
            //0 water, 1 grass, 2 mountain, 3 sand
            new[] { '0', '0', '0', '1', '0', '0', '0', '0', '0', '0', '0', '0', '1', '0', '0', '0'},
            new[] { '0', '0', '0', '1', '0', '0', '0', '0', '0', '1', '1', '1', '1', '1', '1', '0'},
            new[] { '0', '1', '1', '1', '1', '1', '0', '0', '0', '1', '1', '1', '1', '1', '1', '0'},
            new[] { '0', '1', '1', '1', '1', '1', '0', '0', '0', '1', '1', '1', '1', '1', '1', '0'},
            new[] { '0', '1', '1', '1', '1', '1', '1', '1', '1', '1', '1', '1', '1', '1', '1', '0'},
            new[] { '1', '1', '1', '1', '1', '1', '0', '0', '0', '1', '1', '1', '1', '1', '1', '1'},
            new[] { '0', '1', '1', '1', '1', '1', '0', '0', '0', '1', '1', '1', '1', '1', '1', '0'},
            new[] { '0', '0', '1', '0', '0', '0', '0', '0', '0', '1', '1', '1', '1', '1', '1', '0'},
            new[] { '0', '0', '1', '0', '0', '0', '0', '0', '0', '0', '0', '1', '0', '0', '0', '0'},
            new[] { '0', '0', '1', '0', '0', '0', '0', '0', '0', '0', '0', '1', '0', '0', '0', '0'},
            new[] { '0', '0', '1', '1', '1', '1', '1', '1', '0', '0', '1', '1', '1', '1', '0', '0'},
            new[] { '1', '1', '1', '1', '1', '1', '1', '1', '0', '0', '1', '1', '1', '1', '1', '1'},
            new[] { '0', '0', '1', '1', '1', '1', '1', '1', '1', '1', '1', '1', '1', '1', '0', '0'},
            new[] { '0', '0', '1', '1', '1', '1', '1', '1', '0', '0', '1', '1', '1', '1', '0', '0'},
            new[] { '0', '0', '1', '1', '1', '1', '1', '1', '0', '0', '0', '0', '1', '0', '0', '0'},
            new[] { '0', '0', '0', '1', '0', '0', '0', '0', '0', '0', '0', '0', '1', '0', '0', '0'}
        }, periodic: false);
        // Specify the model used for generation
        var model = new OverlappingModel(sample.ToTiles(), 3, 4, true);
        var msize = this.ChunkSize();
        // Set the output dimensions
        var topology = new GridTopology(msize, msize, periodic: false);

        var options = new TilePropagatorOptions();
        var random = new Random(y * 1000 + x);

        options.RandomDouble = () => { return random.NextDouble(); };
        // Acturally run the algorithm
        var propagator = new TilePropagator(model, topology, options);

        var status = propagator.Run();
        if (status != Resolution.Decided) throw new Exception("Undecided");
        var output = propagator.ToValueArray<char>();

        var data = new bool[msize][];
        for (var iy = 0; iy < msize; iy++)
        {
            data[iy] = new bool[msize];
            for (var ix = 0; ix < msize; ix++)
            {
                if (output.Get(ix, iy) == '0')
                {
                    data[iy][ix] = true;
                }
                else
                {
                    data[iy][ix] = false;
                }
            }
        }
        GD.Print(x, y, PositionKey.Key(x, y));
        this.chunks.Add(PositionKey.Key(x, y), data);
    }

    public int ChunkSize()
    {
        return chunkSize;
    }

    public IEnumerable<bool> GetChunk(int x, int y)
    {
        var key = PositionKey.Key(x, y);
        if (key == ulong.MaxValue || !chunks.ContainsKey(key))
        {
            for (var iy = 0; iy < ChunkSize(); iy++)
            {
                for (var ix = 0; ix < ChunkSize(); ix++)
                {
                    yield return true;
                }
            }
        }
        else
        {
            if (chunks.ContainsKey(key))
            {
                var data = chunks[key];
                for (var iy = 0; iy < data.Length; iy++)
                {
                    for (var ix = 0; ix < data[iy].Length; ix++)
                    {
                        yield return data[iy][ix];
                    }
                }
            }
        }
    }

    public IEnumerable<ulong> GetAvailableChunks()
    {
        foreach (var key in chunks.Keys)
        {
            yield return key;
        }
    }

    public float TileScale()
    {
        return 1;
    }
}