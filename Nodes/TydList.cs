namespace Tyd
{
    ///<summary>
    /// Contains an ordered collection of anonymous TydNodes.
    ///</summary>
    public class TydList : TydCollection
    {
        public TydList(string name, TydNode parent, int docLine = -1) : base(name, parent, docLine)
        {
        }

        public override TydNode DeepClone()
        {
            TydList c = new TydList(name, Parent, docLine);
            CopyDataFrom(c);
            return c;
        }

        public override string ToString()
        {
            return Name + "(TydList, " + Count + ")";
        }
    }

}