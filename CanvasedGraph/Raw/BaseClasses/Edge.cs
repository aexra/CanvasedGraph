using CanvasedGraph.Raw.Interfaces;
using System;

namespace CanvasedGraph.Raw;
public class Edge : IEdge
{
    public Node Left;
    public Node Right;
    public string Weight;
    public bool IsLoop => Left == Right;

    public Edge(Node left, Node right, string weight)
    {
        Left = left;
        Right = right;
        Weight = weight;
    }

    public void Remove()
    {

    }

    //public int CompareTo(Edge? other)
    //{
    //    //if (other == null) return -1;
    //    //return Right.Name.CompareTo(other.Right.Name);
    //    return Weight.CompareTo(other.Weight);
    //}

    public override string ToString()
    {
        return $"{Left} -> {Weight} -> {Right}";
    }
}