namespace Tyd
{

internal static class Constants
{
    //Constants - public
    public static string    TydFileExtension        = ".tyd";

    //Constants - internal
    internal const char     CommentChar             = '#';
    internal const char     RecordEndChar           = ';';

    internal const char     AttributeStartChar      = '*';
    internal const string   HandleAttributeName     = "handle";
    internal const string   SourceAttributeName     = "source";
    internal const string   AbstractAttributeName   = "abstract";
    internal const string   NoInheritAttributeName  = "noinherit";

    internal const char     TableStartChar          = '{';
    internal const char     TableEndChar            = '}';
    internal const char     ListStartChar           = '[';
    internal const char     ListEndChar             = ']';
    
    internal const string   SymbolChars             = "_-abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
    internal const string   NullValueString         = "null";
}


}