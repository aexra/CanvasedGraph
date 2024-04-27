﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using CanvasedGraph.UI.Controls;
using Windows.Storage;
using SelectionMode = CanvasedGraph.Enums.SelectionMode;
using CanvasedGraph.Structures;
using CanvasedGraph.Raw;

namespace CanvasedGraph;
public class Constructor
{
    // COMPTIME CONSTANTS
    public static readonly int VertexZ = 10;
    public static readonly int EdgeZ = 0;

    // INPUT PROPS
    public Canvas Canvas;

    // FLAGS
    public bool ReadOnly = false;
    public bool Oriented = false;

    // OTHERS
    public List<Vertex> Nodes => Canvas.Children.Where(x => x is Vertex).Cast<Vertex>().ToList();
    public List<Edge> Edges = new();
    public Vertex? SelectedNode => GetSelectedNode();
    public List<Vertex>? SelectedNodes => GetSelectedNodes();
    public SelectionMode SelectionMode = SelectionMode.None;
    public Queue<Action<Vertex, bool>> SelectionRequests = new();

    // EVENT HANDLERS
    public delegate void NodeCreatedHandler(Vertex node);
    public delegate void NodeRemovedHandler(Vertex node);
    public delegate void NodeSelectedHandler(Vertex node);
    public delegate void NodeRenamedHandler(Vertex node, string oldName);
    public delegate void EdgeCreatedHandler(Edge edge);
    public delegate void EdgeRemovedHandler(Edge edge);
    public delegate void GraphClearedHandler();
    public delegate void LoadedHandler(string path);

    // EVENTS
    public event NodeCreatedHandler? NodeCreated;
    public event NodeRemovedHandler? NodeRemoved;
    public event NodeSelectedHandler? NodeSelected;
    public event NodeRenamedHandler? NodeRenamed;
    public event EdgeCreatedHandler? EdgeCreated;
    public event EdgeRemovedHandler? EdgeRemoved;
    public event GraphClearedHandler? GraphCleared;
    public event LoadedHandler? Loaded;


    // CONSTRUCTORS
    public Constructor(Canvas canvas)
    {
        Canvas = canvas;

        var transparentColor = Color.Transparent;
        Canvas.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(transparentColor.A, transparentColor.R, transparentColor.G, transparentColor.B));

        Canvas.PointerPressed += PointerPressed;
    }

    // POINTER EVENTS
    private void PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (ReadOnly) return;
        if (SelectedNode != null)
        {
            DeselectAllNodes();
            return;
        }
        var pos = e.GetCurrentPoint(Canvas).Position;
        var props = e.GetCurrentPoint(Canvas).Properties;

        if (props.IsLeftButtonPressed) NewNode(pos.X, pos.Y);

    }

    // GRAPH MANIPULATION METHODS
    public void Clear()
    {
        Canvas.Children.Clear();
        Edges.Clear();
        SelectionRequests.Clear();
        GraphCleared?.Invoke();
    }
    public void RequestSelection(Action<Vertex, bool> selected)
    {
        SelectionRequests.Enqueue(selected);
    }

    // NODES MANIPULATION METHODS
    public Vertex? NewNode(double x, double y, string? name = null)
    {
        var node = new Vertex(new Vector2((float)x, (float)y), this);

        if (CheckNodeCollisions(node)) return null;

        if (node.Position.X < 0)
        {
            node.Position = new Vector2(0, node.Position.Y);
        }
        if (node.Position.Y < 0)
        {
            node.Position = new Vector2(node.Position.X, 0);
        }
        if (node.Position.X > Canvas.ActualWidth - 40)
        {
            node.Position = new Vector2((float)Canvas.ActualWidth - 40, node.Position.Y);
        }
        if (node.Position.Y > Canvas.ActualHeight - 40)
        {
            node.Position = new Vector2(node.Position.X, (float)Canvas.ActualHeight - 40);
        }

        if (name == null) node.Title = GetUniqueName();
        else node.Title = name;

        Canvas.Children.Add(node);

        Canvas.SetLeft(node, node.Position.X);
        Canvas.SetTop(node, node.Position.Y);
        Canvas.SetZIndex(node, VertexZ);

        NodeCreated?.Invoke(node);

        return node;
    }
    public void RemoveNode(Vertex node)
    {
        List<Edge> toDelete = new();
        foreach (var edge in Edges)
        {
            if (edge.Left == node || edge.Right == node)
            {
                toDelete.Add(edge);
            }
        }
        toDelete.ForEach(RemoveEdge);
        Canvas.Children.Remove(node);
        NodeRemoved?.Invoke(node);
    }
    public void ConnectNodes(Vertex left, Vertex right, string weight)
    {
        NewEdge(left, right, weight);
    }
    public void ConnectNodes(string left, string right, string weight)
    {
        var l = GetNode(left);
        var r = GetNode(right);
        if (l == null || r == null) return;
        NewEdge(l, r, weight);
    }
    public void DeselectAllNodes()
    {
        foreach (var child in Canvas.Children)
        {
            if (child is Vertex node)
            {
                node.Deselect();
            }
        }
    }
    public void LockAllNodesPosition()
    {
        foreach (var node in Nodes)
        {
            node.LockPosition();
        }
    }
    public void UnlockAllNodesPosition()
    {
        foreach (var node in Nodes)
        {
            node.UnlockPosition();
        }
    }
    public async Task<bool> RenameNode(string target, string name)
    {
        var node = GetNode(target);
        if (node != null)
        {
            if (IsNameValid(name))
            {
                var oldName = node.Title;
                node.Title = name;
                NodeRenamed?.Invoke(node, oldName);
                return true;
            }
            else
            {
                ContentDialog errorDialog = new ContentDialog();

                errorDialog.XamlRoot = Canvas.XamlRoot;
                errorDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                errorDialog.Title = "Недопустимое имя вершины: повторяется или пустое";
                errorDialog.CloseButtonText = "Ок";
                errorDialog.DefaultButton = ContentDialogButton.Close;

                await errorDialog.ShowAsync();

                return false;
            }
        }
        else
        {
            ContentDialog errorDialog = new ContentDialog();

            errorDialog.XamlRoot = Canvas.XamlRoot;
            errorDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            errorDialog.Title = "Вершина с таким именем не найдена";
            errorDialog.CloseButtonText = "Ок";
            errorDialog.DefaultButton = ContentDialogButton.Close;

            await errorDialog.ShowAsync();

            return false;
        }
    }

    // EDGES MANIPULATION METHODS
    public void NewEdge(Vertex left, Vertex right, string weight)
    {
        var edge = new Edge(left, right, weight, this);
        Edges.Add(edge);
        if (Oriented)
        {
            foreach (var edgee in Edges)
            {
                if (edgee.Left == edge.Right && edgee.Right == edge.Left && !edge.IsLoop)
                {
                    edgee.ToArc();
                    edge.ToArc();
                    break;
                }
            }
        }
        edge.UpdatePath();
        UpdateConnectedEdges(edge.Left);
        Canvas.Children.Add(edge.PathObject);
        Canvas.Children.Add(edge.WeightBox);
        Canvas.SetZIndex(edge.PathObject, EdgeZ);
        EdgeCreated?.Invoke(edge);
    }
    public void RemoveEdge(Edge edge)
    {
        Edges.Remove(edge);
        if (edge.IsLoop)
        {
            var deletedIndex = edge.LoopIndex;
            edge.Left.Loops--;
            foreach (var edgee in Edges)
            {
                if (edgee.IsLoop && edgee.Left == edge.Left && edgee.LoopIndex > deletedIndex)
                {
                    edgee.LoopIndex--;
                }
            }
            UpdateConnectedEdges(edge.Left);
        }
        foreach (var child in Canvas.Children)
        {
            if (child is Microsoft.UI.Xaml.Shapes.Path path && edge.PathObject == path)
            {
                Canvas.Children.Remove(child);
                break;
            }
        }
        foreach (var child in Canvas.Children)
        {
            if (child is TextBox box && edge.WeightBox == box)
            {
                Canvas.Children.Remove(child);
                break;
            }
        }
        if (edge.Left != edge.Right)
        {
            foreach (var edgee in Edges)
            {
                if (edgee.Left == edge.Right && edgee.Right == edge.Left)
                {
                    edgee.ToLine();
                    UpdateEdge(edgee);
                    break;
                }
            }
        }
        EdgeRemoved?.Invoke(edge);
    }
    public void UpdateAllEdges()
    {
        foreach (var edge in Edges)
        {
            UpdateEdge(edge);
        }
    }
    public void UpdateConnectedEdges(Vertex node)
    {
        foreach (var edge in Edges)
        {
            if (edge.Left == node || edge.Right == node)
            {
                UpdateEdge(edge);
            }
        }
    }
    public void UpdateEdge(Edge edge)
    {
        foreach (var child in Canvas.Children)
        {
            if (child is Microsoft.UI.Xaml.Shapes.Path path && edge.PathObject == path)
            {
                Canvas.Children.Remove(child);
                Canvas.Children.Add(edge.UpdatePath());
                Canvas.SetZIndex(edge.PathObject, EdgeZ);
                break;
            }
        }
    }

    // HELPERS
    public Vertex? GetSelectedNode()
    {
        if (SelectionMode != SelectionMode.Single) return null;
        foreach (var child in Canvas.Children)
        {
            if (child is Vertex node)
            {
                if (node.IsSelected)
                {
                    return node;
                }
            }
        }
        return null;
    }
    public List<Vertex>? GetSelectedNodes()
    {
        if (SelectionMode != SelectionMode.Multiple) return null;
        List<Vertex> nodes = new();
        foreach (var child in Canvas.Children)
        {
            if (child is Vertex node)
            {
                if (node.IsSelected)
                {
                    nodes.Add(node);
                }
            }
        }
        return nodes;
    }
    public Vertex? GetNode(string name)
    {
        foreach (var element in Canvas.Children)
        {
            if (element is Vertex node)
            {
                if (node.Title == name) return node;
            }
        }
        return null;
    }
    public string GetUniqueName()
    {
        var counter = 0;
        while (true)
        {
            var found = false;
            foreach (var element in Canvas.Children)
            {
                if (element is Vertex node && node.Title == $"q{counter}")
                {
                    found = true;
                    break;
                }
            }
            if (found)
            {
                counter++;
            }
            else
            {
                return "q" + counter.ToString();
            }
        }
    }
    public bool IsNameUnique(string name)
    {
        foreach (var element in Canvas.Children)
        {
            if (element is Vertex node && node.Title == name) return false;
        }
        return true;
    }
    public bool IsNameValid(string name)
    {
        return IsNameUnique(name) && !string.IsNullOrWhiteSpace(name);
    }
    public bool IsEdgeExists(Edge edge)
    {
        if (Oriented)
        {
            foreach (var edgee in Edges)
            {
                if (edgee.Left == edge.Left && edgee.Right == edge.Right) return true;
            }
        }
        else
        {
            foreach (var edgee in Edges)
            {
                if (edgee.Left == edge.Left && edgee.Right == edge.Right || edgee.Left == edge.Right && edgee.Right == edge.Left) return true;
            }
        }
        return false;
    }
    public bool IsEdgeExists(Vertex left, Vertex right)
    {
        if (Oriented)
        {
            foreach (var edgee in Edges)
            {
                if (edgee.Left == left && edgee.Right == right) return true;
            }
        }
        else
        {
            foreach (var edgee in Edges)
            {
                if (edgee.Left == left && edgee.Right == right || edgee.Left == right && edgee.Right == left) return true;
            }
        }
        return false;
    }
    public bool CheckNodeCollisions(Vertex c)
    {
        foreach (var element in Canvas.Children)
        {
            if (element is Vertex node)
            {
                if (node == c) continue;
                var d = Math.Sqrt(Math.Pow(c.Position.X - node.Position.X, 2) + Math.Pow(c.Position.Y - node.Position.Y, 2));
                if (d < c.Radius + node.Radius)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public Vertex? GetStartNode()
    {
        foreach (var node in Nodes)
        {
            if (node.SubState == Enums.NodeSubState.Start) return node;
        }
        return null;
    }
    public Vertex? GetEndNode()
    {
        foreach (var node in Nodes)
        {
            if (node.SubState == Enums.NodeSubState.End) return node;
        }
        return null;
    }

    // RAW GRAPH THINGS
    public Graph ToRaw()
    {
        var graph = new Graph() { Oriented=Oriented };

        // Формируем граф
        // Добавим все вершины
        foreach (var node in Nodes)
        {
            graph.AddNode(new Node(node.Title));
            graph.Nodes.Last().SubState = node.SubState;
        }
        // Добавим все связи
        foreach (var edge in Edges)
        {
            var node1 = graph.GetNode(edge.Left.Title);
            var node2 = graph.GetNode(edge.Right.Title);
            if (!graph.IsConnectionExists(node1, node2))
            {
                node1.Connect(node2, edge.Weight, Oriented);
            }
        }

        return graph;
    }
    public void FromRaw(Graph graph)
    {
        var offset = 0;
        foreach (var node in graph.Nodes)
        {
            var n = NewNode(offset += 60, offset, node.Name);
            if (n != null) n.SubState = node.SubState;
        }
        foreach (var node in graph.Nodes)
        {
            foreach (var edge in node.Edges)
            {
                var l = GetNode(node.Name);
                var r = GetNode(edge.Right.Name);
                if (l == null || r == null) continue;
                if (!IsEdgeExists(l, r))
                    ConnectNodes(l, r, edge.Weight);
                else
                {
                    var e = Edges.Find(x => x.Left == l && x.Right == r);
                    if (e != null)
                    {
                        var ww = new string[2] { e.Weight, edge.Weight };
                        ww.ToList().Sort();
                        e.Weight = $"{string.Join(",", ww)}";
                    }
                }
            }
        }
    }
    public RawGraphStruct GetRawGraphStruct()
    {
        var s = new RawGraphStruct();

        s.Oriented = Oriented;
        s.Nodes = new();
        s.States = new();
        s.Edges = new();

        foreach (var node in Nodes)
        {
            s.Nodes.Add(node.Title, new float[] { node.Position.X, node.Position.Y });
            s.States.Add(node.Title, node.SubState.ToString());
        }
        foreach (var edge in Edges)
        {
            s.Edges.Add(new string[] { edge.Left.Title, edge.Right.Title, edge.Weight });
        }

        return s;
    }

    // GRAPH EVENTS
    public bool _NodeSelecting_(Vertex node, bool ephemeral = false)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                DeselectAllNodes();
                _NodeSelected_(node, ephemeral);
                return true;
            case SelectionMode.Multiple:
                _NodeSelected_(node, ephemeral);
                return true;
            default:
                return false;
        }
    }
    public void _NodeSelected_(Vertex node, bool ephemeral = false)
    {
        node.Selected(ephemeral);
        NodeSelected?.Invoke(node);
    }

    // JSON CONVERTER
    public bool FromJson(StorageFile file)
    {
        Clear();

        using (StreamReader r = new StreamReader(file.Path))
        {
            var json = r.ReadToEnd();
            RawGraphStruct g;
            try
            {
                g = JsonConvert.DeserializeObject<RawGraphStruct>(json);
                try
                {
                    Oriented = g.Oriented;
                    foreach (var name in g.Nodes.Keys)
                    {
                        NewNode(g.Nodes[name][0], g.Nodes[name][1], name);
                    }
                    foreach (var edgeAry in g.Edges)
                    {
                        ConnectNodes(edgeAry[0], edgeAry[1], edgeAry[2]);
                    }
                    foreach (var node in g.States.Keys)
                    {
                        var state_name = g.States[node];
                        var node_c = GetNode(node);
                        if (node_c == null) throw new Exception();
                        switch (state_name)
                        {
                            case "Start":
                                node_c.SubState = Enums.NodeSubState.Start;
                                break;
                            case "End":
                                node_c.SubState = Enums.NodeSubState.End;
                                break;
                            case "Universal":
                                node_c.SubState = Enums.NodeSubState.Universal;
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch { Clear(); return false; }
            }
            catch { Clear(); return false; }
        }

        Loaded?.Invoke(file.Path);
        return true;
    }
    public string? ToJson()
    {
        var s = GetRawGraphStruct();
        var json = JsonConvert.SerializeObject(s);
        return json;
    }
}