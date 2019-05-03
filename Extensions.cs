namespace Tyd
{

public static class Extensions
{
    public static bool IsNullValueNode( this TydNode node )
    {
        return node is TydString nodeStr && nodeStr.Value == null;
    }
}

}