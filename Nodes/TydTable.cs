namespace Tyd
{
    ///<summary>
    /// Contains an ordered collection of named TydNodes.
    ///</summary>
    public class TydTable : TydCollection
    {
        public TydNode this[string name]
        {
            get
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].Name == name)
                        return nodes[i];
                }
                return null;
            }
        }

        public TydTable(string name, TydNode parent, int docLine = -1) : base(name, parent, docLine)
        {
        }

        public override TydNode DeepClone()
        {
            TydTable c = new TydTable(name, Parent, docLine);
            CopyDataFrom(c);
            return c;
        }

        public override string ToString()
        {
            return Name + "(TydTable, " + Count + ")";
        }
    }
}