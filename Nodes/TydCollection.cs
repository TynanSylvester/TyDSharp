using System.Collections;
using System.Collections.Generic;

namespace Tyd
{

///<summary>
/// A TydNode that contains a collection of sub-nodes.
///</summary>
public abstract class TydCollection : TydNode, IEnumerable<TydNode>
{
    //Data
    protected List<TydNode> nodes = new List<TydNode>();
    protected string        attClass;
    protected string        attHandle;
    protected string        attSource;
    protected bool          attAbstract;

    //Properties
    public int              Count           =>nodes.Count;
    public List<TydNode>    Nodes           =>nodes;
    public string AttributeClass   {get=>attClass;       set=>attClass    = value;}
    public string AttributeHandle  {get=>attHandle;      set=>attHandle   = value;}
    public string AttributeSource  {get=>attSource;      set=>attSource   = value;}
    public bool   AttributeAbstract{get=>attAbstract;    set=>attAbstract = value;}
    public TydNode          this[int index] =>nodes[index];
    IEnumerator IEnumerable.GetEnumerator() =>GetEnumerator();
    public IEnumerator<TydNode> GetEnumerator()
    {
        foreach( TydNode n in nodes )
        {
            yield return n;
        }
    }

    public TydCollection(string name, TydNode parent, int docLine=-1) : base(name, parent, docLine)
    {
    }

    public void SetupAttributes( string attClass, string attHandle, string attSource, bool attAbstract )
    {
        this.attClass = attClass;
        this.attHandle = attHandle;
        this.attSource = attSource;
        this.attAbstract = attAbstract;
    }

    ///<summary>
    /// Add a node as a child of this node, and link it as a parent.
    ///</summary>
    public void AddChild( TydNode node )
    {
        nodes.Add(node);
        node.Parent = this;
    }

    protected void CopyDataFrom( TydCollection other )
    {
        other.docIndexEnd = docIndexEnd;
        other.attClass = attClass;
        other.attHandle = attHandle;
        other.attSource = attSource;
        other.attAbstract = attAbstract;
        for( int i=0; i<nodes.Count; i++ )
        {
            other.AddChild( nodes[i].DeepClone() );
        }
    }
}

}