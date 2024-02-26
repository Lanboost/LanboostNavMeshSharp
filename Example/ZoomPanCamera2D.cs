using Godot;
using System;

public partial class ZoomPanCamera2D : Node
{
	[Export]
	float zoomSpeed = 0.2f;
	float zoomLevel = 0;

	bool drag = false;

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);

		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.IsPressed() && !mouseEvent.IsEcho())
			{
				var mousePos = mouseEvent.Position;
				if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
				{
					zoomAtPoint(1, mousePos);

				}
				if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
				{
					zoomAtPoint(-1, mousePos);
				}

				if (mouseEvent.ButtonIndex == MouseButton.Right)
				{
					drag = true;
				}

			}
			if (mouseEvent.IsReleased() && mouseEvent.ButtonIndex == MouseButton.Right)
			{
				drag = false;
			}

		}
		if (@event is InputEventMouseMotion mouseMotion)
		{
			if (drag)
			{

				var viewport = this.GetViewport();
				var camera = viewport.GetCamera2D();
				camera.Offset -= mouseMotion.Relative/camera.Zoom;
			}
        }
	}

	

	void zoomAtPoint(int zoomChange, Vector2 mousePosition) {

		var viewport = this.GetViewport();
		var camera = viewport.GetCamera2D();
		var viewportSize = viewport.GetVisibleRect().Size;

		var previousZoom = camera.Zoom;


		var mouseOffset = mousePosition-(viewportSize * 0.5f);


		var trueMousePos = camera.Offset;
		trueMousePos += mouseOffset / previousZoom;

		zoomLevel += zoomSpeed * zoomChange; 


        camera.Zoom = Mathf.Pow(2,zoomLevel)*Vector2.One;

		
		var pixelsCurrentOffset = mouseOffset / camera.Zoom;
		camera.Offset = trueMousePos- pixelsCurrentOffset;

    }
}
