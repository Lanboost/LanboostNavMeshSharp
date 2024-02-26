using Godot;
using System;

public partial class NavMeshRenderer : Node
{
    [Export]
    public Main main;

    [Export]
    public TileMap tileMap;

    [Export]
    public TileMap tileMapSubdivided;

    [Export]
    public int layer;

    [Export]
    public PointDrawer PointDrawer;


    public override void _EnterTree()
    {
        base._EnterTree();

        main.NavMeshUpdatedOnChanged += World_DisplayedCollisionLayerOnChanged;
        main.PathOnChanged += Main_PathOnChanged;
        main.StartPositionOnChanged += Main_PathOnChanged;
        main.EndPositionOnChanged += Main_PathOnChanged;

    }

    private void Main_PathOnChanged(object sender)
    {
        var startC = Colors.Blue;
        var endC = Colors.Yellow;

        float startHue;
        float endHue;
        float sat;
        float value;

        startC.ToHsv(out startHue, out sat, out value);
        endC.ToHsv(out endHue, out sat, out value);

        PointDrawer.ClearPoints();
        
        if (main.Path != null)
        {
            var index = 0.0f;
            foreach(var p in main.Path)
            {
                PointDrawer.AddPoint(new Vector2(p.X, p.Y), Color.FromHsv(Godot.Mathf.Lerp(startHue, endHue, index / (main.Path.Count - 1)), 1, 1));
                index++;
            }
        }
        else
        {
            PointDrawer.AddPoint(main.StartPosition, startC);
            PointDrawer.AddPoint(main.EndPosition, endC);
        }
    }

    public void DrawNavMeshRect(TileMap tilemap, NavMeshRect rect, int layer, int sprite, Vector2I spritePos)
    {
        //TODO
        for (var cy = rect.sy; cy < rect.sy + rect.height; cy++)
        {
            for (var cx = rect.sx; cx < rect.sx + rect.width; cx++)
            {

                //tilemap.SetCell(layer, new Vector2I(cx, cy), sprite, spritePos);
                var tsize = tilemap.TileSet.TileSize* tilemap.Scale;
                PointDrawer.AddRect(new Vector2(rect.sx, rect.sy)* tsize, new Vector2(rect.width, rect.height)* tsize, Colors.Red);
            }
        }
    }

    private void World_DisplayedCollisionLayerOnChanged(object sender)
    {
        GD.Print("NAVMESH CHANGED");
        PointDrawer.ClearRects();

        /*if (!main.World.DisplayCollider)
        {
            return;
        }

        object currworld = main.World.baseWorld;

        if (main.World.DisplayedCollisionLayer > 0)
        {
            currworld = main.World.worldCollisionSteps[main.World.DisplayedCollisionLayer - 1];
        }*/

        //if (currworld is IBaseCollisionWorld baseWorld)
        //{

        var tm = tileMap;
        /*if (main.navMesh.ChunkSize() > 64)
        {
            tm = tileMapSubdivided;
        }*/

        var index = 0;
        foreach (var chunk in main.navMesh.AllChunks())
        {
            foreach(var rects in chunk.navMeshRects)
            {
                GD.Print(rects);
                DrawNavMeshRect(tm, rects, layer, 2, new Vector2I(index %10, index / 10));
                index++;
                if(index >= 100)
                {
                    index = 0;
                }
            }
        }
    }

}
