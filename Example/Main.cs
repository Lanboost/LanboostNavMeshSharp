using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie;
using Godot;
using System;
using System.Collections.Generic;
using DeBroglie.Rot;
using Lanboost.PathFinding.Astar;
using Lanboost.PathFinding.Graph;
using System.Collections;
using System.Drawing;

public delegate void PropertyChangeEvent(object sender);
public class WorldData
{
    public object baseWorld;

    public List<object> worldCollisionSteps = new List<object>();

    public WorldData(object baseWorld)
    {
        this.baseWorld = baseWorld;
    }

    

    protected int _DisplayedCollisionLayer = -1;
    #region property change event
    public int DisplayedCollisionLayer
    {
        get => _DisplayedCollisionLayer;
        set
        {
            if (_DisplayedCollisionLayer == value)
            {
                return;
            }
            _DisplayedCollisionLayer = value;
            DisplayedCollisionLayerOnChanged?.Invoke(this);
        }
    }
    public event PropertyChangeEvent DisplayedCollisionLayerOnChanged;
    #endregion


    protected bool _DisplayCollider;
    #region property change event
    public bool DisplayCollider
    {
        get => _DisplayCollider;
        set
        {
            if (_DisplayCollider == value)
            {
                return;
            }
            _DisplayCollider = value;
            DisplayColliderOnChanged?.Invoke(this);
        }
    }
    public event PropertyChangeEvent DisplayColliderOnChanged;
    #endregion




}

public partial class Main : Node2D
{
    float[][] HeightData;

    
    public NavMesh navMesh;



    protected WorldData _World;
    #region property change event
    public WorldData World
    {
        get => _World;
        set
        {
            if (_World == value)
            {
                return;
            }
            _World = value;
            WorldOnChanged?.Invoke(this);
        }
    }
    public event PropertyChangeEvent WorldOnChanged;
    #endregion



    protected bool _NavMeshUpdated;
    #region property change event
    public bool NavMeshUpdated
    {
        get => _NavMeshUpdated;
        set
        {
            if (_NavMeshUpdated == value)
            {
                return;
            }
            _NavMeshUpdated = value;
            NavMeshUpdatedOnChanged?.Invoke(this);
        }
    }
    public event PropertyChangeEvent NavMeshUpdatedOnChanged;
    #endregion



    protected Vector2 _StartPosition = Vector2.Inf;
    #region property change event
    public Vector2 StartPosition
    {
        get => _StartPosition;
        set
        {
            if (_StartPosition == value)
            {
                return;
            }
            _StartPosition = value;
            StartPositionOnChanged?.Invoke(this);
        }
    }
    public event PropertyChangeEvent StartPositionOnChanged;
    #endregion


    protected Vector2 _EndPosition = Vector2.Inf;
    #region property change event
    public Vector2 EndPosition
    {
        get => _EndPosition;
        set
        {
            if (_EndPosition == value)
            {
                return;
            }
            _EndPosition = value;
            EndPositionOnChanged?.Invoke(this);
        }
    }
    public event PropertyChangeEvent EndPositionOnChanged;
    #endregion


    protected List<NavigationPoint> _Path;
    #region property change event
    public List<NavigationPoint> Path
    {
        get => _Path;
        set
        {
            if (_Path == value)
            {
                return;
            }
            _Path = value;
            PathOnChanged?.Invoke(this);
        }
    }
    public event PropertyChangeEvent PathOnChanged;
    #endregion




    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        toggleCollisionDisplay.Pressed += () =>
        {
            this.World.DisplayCollider = !this.World.DisplayCollider;
        };

        toggleCollisionlayerButton.ItemSelected += (index) =>
        {
            this.World.DisplayedCollisionLayer = (int)index;
        };

        this.WorldOnChanged += (sender) =>
        {
            while (toggleCollisionlayerButton.ItemCount > 0)
            {
                toggleCollisionlayerButton.RemoveItem(0);
            }
            toggleCollisionlayerButton.AddItem("Base colliders");
            foreach (var layer in World.worldCollisionSteps)
            {
                toggleCollisionlayerButton.AddItem(layer.GetType().Name);
            }

        };


        var baseworld = new WaveFunctionCollapseWorld(64);
        
        for(int y = 0; y<2; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                baseworld.GenerateChunk(x, y);
            }
        }

        var wd = new WorldData(baseworld);
        var subdivide = new SubdivideCollisionWorld(baseworld, 3);
        wd.worldCollisionSteps.Add(subdivide);
        var worldCollision = new ExpandedTileCollisionWorld(subdivide);
        wd.worldCollisionSteps.Add(worldCollision);
        World = wd;

        navMesh = new NavMesh(worldCollision);

        
        TileMapEvents.TileOnMouse += (tmap, tile, offset, buttonEvent) =>
        {
            if (buttonEvent.ButtonIndex == MouseButton.Left && buttonEvent.IsPressed())
            {
                if (buttonEvent.ShiftPressed)
                {
                    this.StartPosition = tile + offset;
                    UpdatePath();
                }
                else if (buttonEvent.AltPressed)
                {
                    this.EndPosition = tile + offset;
                    UpdatePath();
                }
                else
                {
                    var x = tile.X / (worldCollision.ChunkSize()* worldCollision.TileScale());
                    var y = tile.Y / (worldCollision.ChunkSize() * worldCollision.TileScale());
                    navMesh.GenerateChunk((int)x, (int)y);
                    navMesh.GenerateAllRemoteLinks((int)x, (int)y);
                    NavMeshUpdated = !NavMeshUpdated;
                }
            }
        };
    }


    int mode = 0;


    void UpdatePath()
    {
        GD.Print(StartPosition);
        GD.Print(EndPosition);
        if (StartPosition == Vector2.Inf || EndPosition == Vector2.Inf)
        {
            return;
        }


        var path = navMesh.FindPath(new NavigationPoint(StartPosition.X, StartPosition.Y,0), new NavigationPoint(EndPosition.X, EndPosition.Y, 0));
        if (path != null)
        {
            this.Path = path;
            GD.Print(path.Count);
        }
        else {
            GD.Print("No path");
        }
    }
    

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        base._UnhandledKeyInput(@event);
        //SmoothTerrain(5, 5, 2, 1);
        //UpdateMap();

        if (@event is InputEventKey keyEvent)
        {
            if (ssfStates.Count > 0)
            {
                if (keyEvent.IsPressed() && keyEvent.Keycode == Key.Q)
                {
                    ssfindex--;
                    if (ssfindex == 0)
                    {
                        ssfindex = ssfStates.Count - 1;
                    }
                }
                if (keyEvent.IsPressed() && keyEvent.Keycode == Key.E)
                {
                    ssfindex++;
                    ssfindex = ssfindex % ssfStates.Count;
                }
                UpdateSSFState();
            }
        }
    }

    void SetCell(int x, int y, int value)
    {
        var v = value - 1;
        var vy = (int) v / 10;
        var vx = v % 10;
        tilemap.SetCell(0, new Vector2I(x, y), 2, new Vector2I(vx, vy));
    }

    void UpdateMap()
    {


        for (int y = 0; y < HeightData.Length; y++)
        {
            for (int x = 0; x < HeightData[y].Length; x++)
            {
                SetCell(x, y, (int)HeightData[y][x]);
            }
        }
    }

    public void SmoothTerrain1(float cordX, float cordY, int BrushSize, float Opacity)
    {
        var markOffset = new int[][]
        {
            new int[] { 1, 0},
            new int[] { 1, 1},
            new int[] { 0, 1},
            new int[] {-1, 1},
            new int[] {-1, 0},
            new int[] {-1,-1},
            new int[] { 0,-1},
            new int[] { 1,-1},
        };
        
        // TODO iterate over all markOffsets, and combine value etc
    }

    public void SmoothTerrain(float cordX, float cordY, int BrushSize, float Opacity)
    {
        float[,] newHeightData;

        // Note: MapWidth and MapHeight should be equal and power-of-two values 
        newHeightData = new float[(BrushSize * 2) + 1, (BrushSize * 2) + 1];

        for (int x = (int)cordX - BrushSize; x <= cordX + BrushSize; x++)
        {
            for (int y = (int)cordY - BrushSize; y <= cordY + BrushSize; y++)
            {
                int adjacentSections = 0;
                float sectionsTotal = 0.0f;

                if ((x - 1) > 0) // Check to left
                {
                    sectionsTotal += HeightData[y][x - 1];
                    adjacentSections++;

                    if ((y - 1) > 0) // Check up and to the left
                    {
                        sectionsTotal += HeightData[y-1][x - 1];
                        adjacentSections++;
                    }

                    if ((y + 1) < cordY + BrushSize) // Check down and to the left
                    {
                        sectionsTotal += HeightData[y+1][x - 1];
                        adjacentSections++;
                    }
                }

                if ((x + 1) < cordX + BrushSize) // Check to right
                {
                    sectionsTotal += HeightData[y][x + 1];
                    adjacentSections++;

                    if ((y - 1) > 0) // Check up and to the right
                    {
                        sectionsTotal += HeightData[y-1][x + 1];
                        adjacentSections++;
                    }

                    if ((y + 1) < cordY + BrushSize) // Check down and to the right
                    {
                        sectionsTotal += HeightData[y+1][x + 1];
                        adjacentSections++;
                    }
                }

                if ((y - 1) > 0) // Check above
                {
                    sectionsTotal += HeightData[y-1][x];
                    adjacentSections++;
                }

                if ((y + 1) < cordY + BrushSize) // Check below
                {
                    sectionsTotal += HeightData[y+1][x];
                    adjacentSections++;
                }

                newHeightData[y, x] = (HeightData[y][x] + (sectionsTotal / adjacentSections)) * Opacity;//0.5f;
            }
        }


        for (int x = (int)cordX - BrushSize; x <= cordX + BrushSize; x++)
        {
            for (int y = (int)cordY - BrushSize; y <= cordY + BrushSize; y++)

                HeightData[(int)cordY - BrushSize + y][(int)cordX - BrushSize + x] = newHeightData[y, x];

        }

    }

    [Export]
    public TileMap tilemap;

    [Export]
    public TileMapEvents TileMapEvents;

    public Vector2I start;
    public Vector2I end;

    List<SSFState> ssfStates = new List<SSFState>();
    int ssfindex = 0;

    float zoom;
    public float Zoom
    {
        get { return zoom; }
        set { zoom = value;
        }
    }

    [Export]
    public Line2D pathLine;

    [Export]
    public Line2D funnelLine;

    [Export]
    public Line2D leftLine;

    [Export]
    public Line2D rightLine;

    [Export]
    public Label helpLabel;

    [Export]
    public PointDrawer drawer;

    [Export]
    public Button toggleCollisionDisplay;

    [Export]
    public OptionButton toggleCollisionlayerButton;

    public void UpdateSSFState()
    {
        var state = ssfStates[ssfindex];
        helpLabel.Text = $"Left index: {state.leftIndex}\n"+
            $"Right Index: {state.rightIndex}\n"+
            $"Help: {state.help}";

        pathLine.ClearPoints();
        funnelLine.ClearPoints();
        leftLine.ClearPoints();
        rightLine.ClearPoints();
        drawer.ClearPoints();

        if (state.pointList != null)
        {
            foreach (var p in state.pointList) {
                pathLine.AddPoint(new Vector2(p.X*32, p.Y*32));
            }
        }

        drawer.AddPoint(state.funnel, Colors.Orange);

        if(state.funnelSide != Vector2.Zero)
        {
            funnelLine.AddPoint(state.funnel * 32);
            funnelLine.AddPoint((state.funnel+ state.funnelSide)*32);
        }


        leftLine.AddPoint(state.funnel*32);
        leftLine.AddPoint((state.funnel + state.left) * 32);

        rightLine.AddPoint(state.funnel * 32);
        rightLine.AddPoint((state.funnel + state.right) * 32);


    }

}
