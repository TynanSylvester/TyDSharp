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
        protected string attHandle;
        protected string attSource;
        protected bool attAbstract;
        protected bool attNoInherit;

        //Properties
        public int Count
        {
            get{return nodes.Count;}
        }

        public List<TydNode> Nodes
        {
            get{return nodes;}
        }

        public string AttributeHandle
        {
            get{return attHandle;}
            set{attHandle = value;}
        }

        public string AttributeSource
        {
            get{return attSource;}
            set{attSource = value;}
        }

        public bool AttributeAbstract
        {
            get{return attAbstract;}
            set{attAbstract = value;}
        }

        public bool AttributeNoInherit
        {
            get{return attNoInherit;}
            set{attNoInherit = value;}
        }

        public TydNode this[int index]
        {
            get
            {
                return nodes[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TydNode> GetEnumerator()
        {
            foreach (TydNode n in nodes)
            {
                yield return n;
            }
        }

        public TydCollection(string name, TydNode parent, int docLine = -1) : base(name, parent, docLine)
        {
        }

        public void SetupAttributes(string attHandle, string attSource, bool attAbstract, bool attNoInherit)
        {
            this.attHandle = attHandle;
            this.attSource = attSource;
            this.attAbstract = attAbstract;
            this.attNoInherit = attNoInherit;
        }

        public void AddChild(TydNode node)
        {
            nodes.Add(node);
            node.Parent = this;
        }

        public void InsertChild(TydNode node, int index)
        {
            nodes.Insert(index, node);
            node.Parent = this;
        }

        protected void CopyDataFrom(TydCollection other)
        {
            other.docIndexEnd   = docIndexEnd;
            other.attHandle     = attHandle;
            other.attSource     = attSource;
            other.attAbstract   = attAbstract;
            other.attNoInherit  = attNoInherit;
            for (int i = 0; i < nodes.Count; i++)
            {
                other.AddChild(nodes[i].DeepClone());
            }
        }
    }

}