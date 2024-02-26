using Godot;
using Lanboost.PathFinding.Graph;
using System;

public partial class BaseWorldRenderer : Node
{
    [Export]
    public Main main;

    [Export]
    public TileMap tileMap;

    [Export]
    public int layer;

    public override void _EnterTree()
    {
        base._EnterTree();

        main.WorldOnChanged += Main_WorldOnChanged;
        if (main.World != null)
        {
            Main_WorldOnChanged(null);
        }
    }

    private void Main_WorldOnChanged(object sender)
    {
        tileMap.ClearLayer(layer);

        object currworld = main.World.baseWorld;

        if (currworld is IBaseCollisionWorld baseWorld)
        {
            foreach (var chunkKey in baseWorld.GetAvailableChunks())
            {
                var (x, y) = PositionKey.Explode(chunkKey);

                foreach (var (tx, ty, blocked) in baseWorld.GetChunkSimple(x, y))
                {
                    if (blocked)
                    {
                        tileMap.SetCell(layer,
                            new Vector2I(x * baseWorld.ChunkSize() + tx, y * baseWorld.ChunkSize() + ty),
                            3,
                            new Vector2I(1, 1)
                        );
                    }
                    else
                    {
                        tileMap.SetCell(layer,
                            new Vector2I(x * baseWorld.ChunkSize() + tx, y * baseWorld.ChunkSize() + ty),
                            3,
                            new Vector2I(0, 1)
                        );
                    }
                }
            }
        }
    }
}
