using Godot;
using Lanboost.PathFinding.Graph;
using System;

public partial class TileMapEvents : Node
{
    public delegate void OnTileMouseEvent(TileMap tileMap, Vector2I tile, Vector2 offset, InputEventMouseButton mouseEvent);
    
    public event OnTileMouseEvent TileOnMouse;

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (@event is InputEventMouseButton buttonEvent)
        {
            var viewport = GetViewport();
            var camera = viewport.GetCamera2D();

            var truePosition = (-viewport.GetVisibleRect().Size * 0.5f + buttonEvent.Position) / camera.Zoom;
            truePosition += camera.Offset;

            var tilemap = this.GetParent<TileMap>();

            var tile = tilemap.LocalToMap(tilemap.ToLocal(truePosition));
            var offset = tilemap.ToLocal(truePosition)/tilemap.TileSet.TileSize - tile;



            TileOnMouse?.Invoke(tilemap, tile, offset, buttonEvent);
        }
    }
}
