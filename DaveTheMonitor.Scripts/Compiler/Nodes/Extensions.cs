namespace DaveTheMonitor.Scripts.Compiler.Nodes
{
    internal static class Extensions
    {
        public static StatementNode[] Clone(this StatementNode[] array)
        {
            StatementNode[] nodes = new StatementNode[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                nodes[i] = array[i].Clone();
            }
            return nodes;
        }
    }
}
