using System;
using System.Text;

namespace Tyd
{
public static class TydToText
{
    /*
        Possible future features:
            - Ability to align string values into a single column
            - Ability to write lists/tables with 0 or 1 children on a single line
            - Some way to better control which strings get quotes and which don't
     */

    ///<summary>
    /// Writes a given TydNode, along with all its descendants, as a string, at a given indent level.
    /// This method is recursive.
    ///</summary>
    public static string Write( TydNode node, int indent = 0 )
    {

        //It's a string
        if( node is TydString str )
            return IndentString(indent) + node.Name + " " + StringContentWriteable(str.Value);

        //It's a table
        if( node is TydTable tab )
        {
            StringBuilder sb = new StringBuilder();

            //Intro line
            if( AppendNodeIntro(tab, sb, indent) && tab.Count > 0 )
                sb.AppendLine();

            if( tab.Count == 0 )
                sb.Append("[]");
            else
            {
                //Sub-nodes
                sb.AppendLine( IndentString(indent) + "[");
                for( int i=0; i<tab.Count; i++ )
                {
                    sb.AppendLine( Write(tab[i], indent+1) );
                }
                sb.Append( IndentString(indent) + "]");
            }

            return sb.ToString();
        }

        //It's a list
        if( node is TydList list )
        {
            StringBuilder sb = new StringBuilder();

            //Intro line
            if( AppendNodeIntro(list, sb, indent) && list.Count > 0 )
                sb.AppendLine();

            if( list.Count == 0 )
                sb.Append("{}");
            else
            {
                //Sub-nodes
                sb.AppendLine( IndentString(indent) + "{");
                for( int i=0; i<list.Count; i++ )
                {
                    sb.AppendLine( Write(list[i], indent+1) );
                }
                sb.Append( IndentString(indent) + "}");
            }

            return sb.ToString();
        }

        throw new ArgumentException();
    }

    private static string StringContentWriteable( string value )
    {
        if( value == "" )
            return "\"\"";

        if( value == null )
            return Constants.NullValueString;

        return ShouldWriteWithQuotes(value)
            ? "\"" + EscapeCharsEscapedForQuotedString(value) + "\""
            : value;

        //This is a set of heuristics to try to determine if we should write a string quoted or naked.
        bool ShouldWriteWithQuotes( string s )
        {
            if( value.Length > 40 ) //String is long
                return true;

            if( value[value.Length-1] == '.' ) //String ends with a period. It's probably a sentence
                return true;

            //Check the string character-by-character
            for(int i=0; i<value.Length; i++ )
            {
                var c = value[i];

                //Chars that imply we should use quotes
                //Some of these are heuristics, like space.
                //Some absolutely require quotes, like the double-quote itself. They'll break naked strings if unescaped (and naked strings are always written unescaped).
                //Note that period is not on this list; it commonly appears as a decimal in numbers.
                if( c == ' '
                || c == '\n'
                || c == '\t'
                || c == '"'
                || c == Constants.CommentChar
                || c == Constants.RecordEndChar
                || c == Constants.AttributeStartChar
                || c == Constants.TableStartChar
                || c == Constants.TableEndChar
                || c == Constants.ListStartChar
                || c == Constants.ListEndChar
                )
                    return true;
            }

            return false;
        }

        //Returns string content s with escape chars properly escaped according to Tyd rules.
        string EscapeCharsEscapedForQuotedString( string s )
        {
            return s.Replace("\"", "\\\"")
                    .Replace("#", "\\#");
        }
    }


    private static bool AppendNodeIntro( TydCollection node, StringBuilder sb, int indent )
    {
        bool appendedSomething = false;

        if( node.Name != null )
            AppendWithWhitespace( node.Name );

        if( node.AttributeClass != null )
            AppendWithWhitespace( Constants.AttributeStartChar + Constants.ClassAttributeName + " " + node.AttributeClass );

        if( node.AttributeAbstract )
            AppendWithWhitespace( Constants.AttributeStartChar + Constants.AbstractAttributeName );

        if( node.AttributeHandle != null )
            AppendWithWhitespace( Constants.AttributeStartChar + Constants.HandleAttributeName + " " + node.AttributeHandle );

        if( node.AttributeSource != null )
            AppendWithWhitespace( Constants.AttributeStartChar + Constants.SourceAttributeName + " " + node.AttributeSource );

        return appendedSomething;

        void AppendWithWhitespace( string s )
        {
            sb.Append( (appendedSomething ? " " : IndentString(indent)) + s );
            appendedSomething = true;
        } 
    }

    private static string IndentString(int indent)
    {
        string s = "";
        for( int i=0; i<indent; i++ )
        {
            s += "    ";
        }
        return s;
    }
}

}