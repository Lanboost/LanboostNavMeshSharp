using Godot;
using Lanboost.PathFinding.Graph;
using System;
using System.Collections.Generic;

public partial class PointDrawer : Node
{
    [Export]
    public float scale;

    [Export]
    PackedScene pointPrefab;

    [Export]
    PackedScene rectPrefab;

    Node pointChildren;
    Node rectChildren;

    public override void _EnterTree()
    {
        base._EnterTree();
        if (rectChildren == null)
        {
            rectChildren = new Node();
            this.AddChild(rectChildren);
        }

        if (pointChildren == null)
        {
            pointChildren = new Node();
            this.AddChild(pointChildren);
        }
        
    }

    //List<Vector2> points = new List<Vector2>();
    public void ClearPoints()
    {
        while (pointChildren.GetChildCount() > 0) {
            pointChildren.RemoveChild(pointChildren.GetChild(0));
        }
    }

    public void ClearRects()
    {
        while (rectChildren.GetChildCount() > 0)
        {
            rectChildren.RemoveChild(rectChildren.GetChild(0));
        }
    }

    public void AddPoint(Vector2 point, Color color)
    {
        var size = 6;
        //this.points.Add(point);
        var sprite = pointPrefab.Instantiate<TextureRect>();
        pointChildren.AddChild(sprite);
        sprite.Position = new Vector2(point.X * scale- size/2, point.Y * scale- size / 2);
        sprite.Size = new Vector2(size, size);
        sprite.SelfModulate = color;
    }

    public void AddRect(Vector2 point, Vector2 size, Color color)
    {
        //this.points.Add(point);
        var rect = rectPrefab.Instantiate<NinePatchRect>();
        rectChildren.AddChild(rect);
        rect.Position = new Vector2(point.X, point.Y);
        rect.Size = size;
        rect.SelfModulate = color;
    }
}
