namespace Tyd
{

    public static class Extensions
    {
        public static bool IsNullValueNode(this TydNode node)
        {
            var nodeStr = node as TydString;

            return nodeStr != null && nodeStr.Value == null;
        }
    }
}