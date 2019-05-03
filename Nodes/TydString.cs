namespace Tyd
{

///<summary>
/// Represents a record of a single string. Also used to represent records with null values.
///</summary>
public class TydString : TydNode
{
    //Data
    private string val;

    //Properties
    public string Value{get=>val; set=>this.val = value;}

    public TydString(string name, string val, TydNode parent, int docLine=-1) : base(name, parent, docLine)
    {
        this.val = val;
    }

    public override TydNode DeepClone()
    {
        TydString c = new TydString(name, val, Parent, docLine);
        c.docIndexEnd = docIndexEnd;
        return c;
    }

    public override string ToString()=>$"{(Name!=null?Name:"NullName")}=\"{val}\"";
}

}