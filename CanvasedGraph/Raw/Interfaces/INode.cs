namespace CanvasedGraph.Raw.Interfaces;
public interface INode
{
    public void Connect(Node to, string weight, bool isOriented);
    public void Disconnect(Node node);
    public void Delete();
}