using Godot;
using System;

public partial class CollisionRenderer : Node
{
    [Export]
    public Main main;

    [Export]
    public TileMap tileMap;

    [Export]
    public TileMap tileMapSubdivided;

    [Export]
    public int layer;



    public override void _EnterTree()
    {
        base._EnterTree();

        main.WorldOnChanged += Main_WorldOnChanged;
        Main_WorldOnChanged(null);
    }

    private void Main_WorldOnChanged(object sender)
    {
        //TODO dont be lazy and unsubscribe..
        if (main.World != null)
        {
            main.World.DisplayedCollisionLayerOnChanged += World_DisplayedCollisionLayerOnChanged;
            main.World.DisplayColliderOnChanged += World_DisplayedCollisionLayerOnChanged;
            World_DisplayedCollisionLayerOnChanged(null);
        }
    }

    private void World_DisplayedCollisionLayerOnChanged(object sender)
    {
        tileMap.ClearLayer(layer);
        tileMapSubdivided.ClearLayer(layer);

        if (!main.World.DisplayCollider)
        {
            return;
        }

        object currworld = main.World.baseWorld;

        if(main.World.DisplayedCollisionLayer > 0)
        {
            currworld = main.World.worldCollisionSteps[main.World.DisplayedCollisionLayer-1];
        }

        if (currworld is IBaseCollisionWorld baseWorld)
        {

            var tm = tileMap;
            if (baseWorld.ChunkSize() > 64)
            {
                tm = tileMapSubdivided;
            }

            foreach (var chunkKey in baseWorld.GetAvailableChunks())
            {
                var (x, y) = PositionKey.Explode(chunkKey);

                foreach (var (tx, ty, blocked) in baseWorld.GetChunkSimple(x, y))
                {
                    if (blocked)
                    {
                        tm.SetCell(layer,
                            new Vector2I(x * baseWorld.ChunkSize() + tx, y * baseWorld.ChunkSize() + ty),
                            3,
                            new Vector2I(2, 1)
                        );
                    }
                }
            }
        }
    }
}
