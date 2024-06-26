﻿using System;
using System.Collections.Generic;
using System.Linq;
using CanvasedGraph.Raw.Interfaces;

namespace CanvasedGraph.Raw;
public class Graph : IGraph
{
    public List<Node> Nodes;
    public bool IsEmpty => Nodes.Count == 0;
    public bool Oriented = false;

    public Graph()
    {
        Nodes = new();
    }

    public void Connect(string left, string right, string w)
    {
        var l = GetNode(left);
        var r = GetNode(right);
        if (l == null || r == null) return;
        l.Connect(r, w, Oriented);
    }
    public void AddNode(Node node)
    {
        Nodes.Add(node);
        node.Graph = this;
    }
    public void RemoveNode(Node node)
    {
        Nodes.Remove(node);
    }
    public void Clear()
    {
        Nodes.Clear();
    }
    public Node? GetNode(string name)
    {
        foreach (Node node in Nodes)
        {
            if (node.Name == name) return node;
        }
        return null;
    }
    public bool IsConnectionExists(Node node1, Node node2)
    {
        if (Oriented)
        {
            foreach (var edge in node1.Edges)
            {
                if (edge.Right == node2) return true;
            }
        }
        else
        {
            var edges = node1.Edges.Concat(node2.Edges).ToList();
            foreach (var edge in edges)
            {
                if (edge.Left == node1 && edge.Right == node2 || edge.Left == node2 && edge.Right == node1) return true;
            }
        }
        return false;
    }
    public bool IsConnectionExists(string name1, string name2)
    {
        var node1 = GetNode(name1);
        var node2 = GetNode(name2);
        if (Oriented)
        {
            foreach (var edge in node1.Edges)
            {
                if (edge.Right == node2) return true;
            }
        }
        else
        {
            var edges = node1.Edges.Concat(node2.Edges).ToList();
            foreach (var edge in edges)
            {
                if (edge.Left == node1 && edge.Right == node2 || edge.Left == node2 && edge.Right == node1) return true;
            }
        }
        
        return false;
    }
    public Node? GetStartNode()
    {
        foreach (var node in Nodes)
        {
            if (node.SubState == Enums.NodeSubState.Start) return node;
        }
        return null;
    }
    public Node? GetEndNode()
    {
        foreach (var node in Nodes)
        {
            if (node.SubState == Enums.NodeSubState.End) return node;
        }
        return null;
    }
    public List<Node> GetStartNodes()
    {
        List<Node> nodes = new();
        foreach (var node in Nodes)
        {
            if (node.SubState == Enums.NodeSubState.Start || node.SubState == Enums.NodeSubState.Universal) nodes.Add(node);
        }
        return nodes;
    }
    public List<string> GetWeightsAlphabet(string separator = ",")
    {
        List<string> alphabet = new();

        foreach (var node in Nodes)
        {
            foreach (var edge in node.Edges)
            {
                var letters = edge.Weight.Split(separator);
                foreach (var letter in letters)
                {
                    if (letter != "ε" && !alphabet.Contains(letter))
                    {
                        alphabet.Add(letter);
                    }
                }
            }
        }

        alphabet.Sort();

        return alphabet;
    }
    public string ToLongString()
    {
        var output = "Граф";

        foreach (var node in Nodes.OrderBy(n => n.Name))
        {
            output += $"\n{node.Name}:";
            var sorted_edges = node.Edges.OrderBy(e => e.Left.Name);
            foreach (var edge in sorted_edges)
            {
                output += $" {edge.Right.Name}({edge.Weight});";
            }
        }

        return output;
    }
}