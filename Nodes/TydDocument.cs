using System.Collections.Generic;

namespace Tyd
{

    ///<summary>
    /// Represents an entire Tyd document.
    ///</summary>
    public class TydDocument : TydTable
    {
        ///<summary>
        /// Create a new empty TydDocument.
        ///</summary>
        public TydDocument() : base(null, null, -1)
        {
            this.nodes = new List<TydNode>();
        }

        ///<summary>
        /// Create a new TydDocument from a list of TydNodes.
        ///</summary>
        public TydDocument(IEnumerable<TydNode> nodes) : base(null, null, -1)
        {
            this.nodes = new List<TydNode>();
            this.nodes.AddRange(nodes);
        }

        public override string ToString()
        {
            return Name + "(TydDocument, " + Count + ")";
        }
    }
}