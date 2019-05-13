namespace Tyd
{

    public enum TydNodeType : byte
    {
        String,
        Table,
        Document,
        List,
    }

    ///<summary>
    /// Root class of all Tyd nodes.
    ///</summary>
    public abstract class TydNode
    {
        //Data
        private TydNode parent;
        protected string name;          //Can be null for anonymous nodes

        //Data for error messages
        public int docLine = -1;        //Line in the doc where this node starts
        public int docIndexEnd = -1;    //Index in the doc where this node ends

        //Access
        public TydNode Parent
        {
            get
            {
                return parent;
            }

            set
            {
                parent = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public int LineNumber
        {
            get
            {
                return docLine;
            }
        }

        public string FullTyd
        {
            get
            {
                return TydToText.Write(this);
            }
        }

        //Construction
        public TydNode(string name, TydNode parent, int docLine = -1)
        {
            this.parent = parent;
            this.name = name;
            this.docLine = docLine;
        }

        public abstract TydNode DeepClone();
    }

}