namespace DaveTheMonitor.Scripts.Compiler
{
    internal struct ScriptToken
    {
        public ScriptTokenType Type { get; private set; }
        public string Lexeme { get; private set; }
        public int Pos { get; private set; }

        public static ScriptToken[] Clone(ScriptToken[] array)
        {
            ScriptToken[] tokens = new ScriptToken[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                tokens[i] = array[i];
            }
            return tokens;
        }

        public override string ToString()
        {
            return $"{Type} : {Lexeme} : {Pos}";
        }

        public ScriptToken(string lexeme, ScriptTokenType type, int pos)
        {
            Lexeme = lexeme;
            Type = type;
            Pos = pos;
        }
    }
}
