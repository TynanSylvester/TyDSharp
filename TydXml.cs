using System.Collections.Generic;
using System.Xml;

namespace Tyd
{

///<summary>
/// Utility methods for converting from XML to TyD.
/// To load XML this way, it must be formatted a specific way.
///</summary>
public static class TydXml
{
    ///<summary>
    /// Read an XML document and convert it to a single Tyd node.
    /// This treats the XML root as the root of a single table.
    ///</summary>
    public static TydNode TydNodeFromXmlDocument( XmlDocument xmlDocument )
    {
        return TydNodeFromXmlNode(xmlDocument.DocumentElement, null);
    }

    ///<summary>
    /// Read an XML document and convert it to a sequence of Tyd nodes.
    /// This ignores the XML root and treats each of the root's children as a separate Tyd table.
    ///</summary>
    public static IEnumerable<TydNode> TydNodesFromXmlDocument( XmlDocument xmlDocument )
    {
        foreach( XmlNode xmlChild in xmlDocument.DocumentElement.ChildNodes )
        {
            TydNode newNode = TydNodeFromXmlNode(xmlChild, null);
            if( newNode != null )
                yield return newNode;
        }
    }

    ///<summary>
    /// Convert a single XML tree into a Tyd tree.
    /// If expectName is false, it'll be parsed as a list item.
    ///</summary>
    public static TydNode TydNodeFromXmlNode( XmlNode xmlRoot, TydNode tydParent )
    {
        if( xmlRoot is XmlComment )
            return null;

        string newTydName = xmlRoot.Name != "li"
            ? xmlRoot.Name
            : null;

        //Record attributes here so we can use them later
        string attHandle = null;
        string attSource = null;
        bool attAbstract = false;
        bool attNoInherit = false;
        var xmlAttributes = xmlRoot.Attributes;
        if( xmlAttributes != null )
        {
            foreach( XmlAttribute a in xmlAttributes )
            {
                if( a.Name == "Handle" )
                    attHandle = a.Value;
                else if( a.Name == "Source" )
                    attSource = a.Value;
                else if( a.Name == "Abstract" && a.Value == "True" )
                    attAbstract = true;
                else if( a.Name == "Inherit" && a.Value == "False" )
                    attNoInherit = true;
            }
        }

        if( xmlRoot.ChildNodes.Count == 1 && xmlRoot.FirstChild is XmlText )
        {
            //It's a string
            return new TydString(newTydName, xmlRoot.FirstChild.InnerText, tydParent);
        }
        else if( xmlRoot.HasChildNodes && xmlRoot.FirstChild.Name == "li" )
        {
            //Children are named 'li'
            //It's a list

            TydList tydRoot = new TydList(newTydName, tydParent);
            tydRoot.SetupAttributes(attHandle, attSource, attAbstract, attNoInherit);
            foreach( XmlNode xmlChild in xmlRoot.ChildNodes )
            {
                tydRoot.AddChild( TydNodeFromXmlNode(xmlChild, tydRoot) );
            }
            return tydRoot;
        }
        else
        {
            //This case catches nodes with no children.
            //Note that the case of no children is ambiguous between list and table; we choose list arbitrarily.

            //It's a table
            TydTable tydRoot = new TydTable(newTydName, tydParent);
            tydRoot.SetupAttributes(attHandle, attSource, attAbstract, attNoInherit);
            foreach( XmlNode xmlChild in xmlRoot.ChildNodes )
            {
                tydRoot.AddChild( TydNodeFromXmlNode(xmlChild, tydRoot) );
            }
            return tydRoot;
        }
    }
}

}