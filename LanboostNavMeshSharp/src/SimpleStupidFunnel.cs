using Godot;
using Lanboost.PathFinding.Graph;
using System;
using System.Collections.Generic;

public class SSFState
{
    public Vector2 funnel;
    public Vector2 left;
    public Vector2 right;
    public Vector2 funnelSide;
    public int mode;


    public int leftIndex;
    public int rightIndex;

    public List<Vector2> pointList;
    public string help;

    public SSFState(Vector2 funnel, Vector2 left, Vector2 right, Vector2 funnelSide, int mode, int leftIndex, int rightIndex, List<Vector2> pointList, string help)
    {
        this.funnel = funnel;
        this.left = left;
        this.right = right;
        this.funnelSide = funnelSide;
        this.mode = mode;
        this.leftIndex = leftIndex;
        this.rightIndex = rightIndex;
        this.pointList = pointList;
        this.help = help;
    }
}

public static class ListCopy
{
    public static List<T> Copy<T>(this List<T> list)
    {
        var ret = new List<T>();
        foreach (var item in list)
        {
            ret.Add(item);
        }
        return ret;
    }
}

public class SimpleStupidFunnel
{

    public static List<Vector2> Run(Vector2 start, Vector2 end, List<NavMeshEdge> edges, ref List<SSFState> steps)
    {
        List<Vector2> pointList = new List<Vector2>();
        (var leftPoints, var rightPoints) = CreateFunnelPoints(start, end, edges);

        Vector2 funnel = start;
        var left = leftPoints[0]-funnel;
        var right = rightPoints[0]-funnel;

        int rightIndex = 0;
        int leftIndex = 0;



        if (steps != null)
        {
            steps.Add(
                new SSFState(
                    funnel,
                    left,
                    right,
                    Vector2.Zero,
                    0,
                    0,
                    0,
                    null,
                    "Initial setup"
                )
            );
        }



        while (true)
        {
            if(rightIndex < leftIndex)
            {
                if (rightIndex + 1 >= rightPoints.Count)
                {
                    return pointList;
                }
                StepFunnel(ref funnel, ref right, ref left, ref rightIndex, ref leftIndex, rightPoints, leftPoints, 1, pointList, ref steps);
            }
            else
            {
                if (leftIndex + 1 >= leftPoints.Count)
                {
                    return pointList;
                }
                StepFunnel(ref funnel, ref left, ref right, ref leftIndex, ref rightIndex, leftPoints, rightPoints, -1, pointList, ref steps);
            }
        }
    }

    protected static void StepFunnel(ref Vector2 funnel, 
        ref Vector2 side,
        ref Vector2 otherSide,
        ref int index,
        ref int otherIndex,
        List<Vector2> toStep, 
        List<Vector2> other, 
        int negativeOperatior,
        List<Vector2> result,
        ref List<SSFState> steps
    )
    {
        var newSide = toStep[index + 1] - funnel;

        var value = newSide.Cross(side)* negativeOperatior;

        if (steps != null)
        {
            steps.Add(
                new SSFState(
                    funnel,
                    side,
                    otherSide,
                    newSide,
                    0,
                    index,
                    otherIndex,
                    result.Copy(),
                    "Funnel check"
                )
            );
        }

        if (value < 0)
        {
            funnel = toStep[index];
            result.Add(funnel);
            side = toStep[index + 1] - funnel;
            otherSide = other[otherIndex] - funnel;

            if (steps != null)
            {
                steps.Add(
                    new SSFState(
                        funnel,
                        side,
                        otherSide,
                        Vector2.Zero,
                        0,
                        index,
                        otherIndex,
                        result.Copy(),
                        "Funnel update [check failed]"
                    )
                );
            }
        }
        else
        {

            side = newSide;
            index++;


            if (steps != null)
            {
                steps.Add(
                    new SSFState(
                        funnel,
                        side,
                        otherSide,
                        Vector2.Zero,
                        0,
                        index,
                        otherIndex,
                        result.Copy(),
                        "Funnel update [check success]"
                    )
                );
            }
        }
    }

    public static (List<Vector2>, List<Vector2>) CreateFunnelPoints(Vector2 start, Vector2 end, List<NavMeshEdge> edges)
    {
        // We go from square rects, so we need to calculate which points will be left / right of the funnel.
        // One can thing of this as triangulating the edges.
        // And we need to check that the triangulation is faceing "up" with cross product

        List<Vector2> leftPoints = new List<Vector2>();
        List<Vector2> rightPoints = new List<Vector2>();

        var currentref = start;

        foreach(var edge in edges)
        {
            Vector2 left = edge.left;
            Vector2 right = edge.right;
            //GetEdgeLeftRight(ref currentref, edge.edge, ref left, ref right);

            leftPoints.Add(left);
            rightPoints.Add(right);

            currentref = left;
        }

        leftPoints.Add(end);
        rightPoints.Add(end);

        return (leftPoints, rightPoints);
    }

    //This does not work, we need step direction, if we are right above / below,
    //cross product is 0, and we do not know how to create the path...
    protected static void GetEdgeLeftRight(ref Vector2 reference, NavMeshRect rect, ref Vector2 left, ref Vector2 right)
    {
        if(rect.width == 0)
        {
            left.X = rect.sx;
            left.Y = rect.sy;
            right.X = rect.sx;
            right.Y = rect.sy+rect.height;
        }
        else
        {
            left.X = rect.sx;
            left.Y = rect.sy;
            right.X = rect.sx + rect.width;
            right.Y = rect.sy;
        }


        // Check which start edge is left, and which one is right
        // do this with cross product
        var leftref = left - reference;
        var rightref = right-reference;
        // If cross is negative, swap points
        var cross = leftref.Cross(rightref);
        if (cross < 0)
        {
            var t = left;
            left = right;
            right = t;
        }
        else if(cross == 0)
        {
            // check 

        }
    }

}
