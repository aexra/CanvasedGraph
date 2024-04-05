using System.Collections.Generic;

namespace CanvasedGraph.Structures;
public struct RawGraphStruct
{
    public Dictionary<string, float[]> Nodes;
    public List<string[]> Edges;
    public Dictionary<string, string> States;
}