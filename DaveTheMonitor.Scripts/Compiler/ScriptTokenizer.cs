using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaveTheMonitor.Scripts.Compiler
{
    internal sealed class ScriptTokenizer
    {
        public event ScriptCompilerErrorEventHandler ErrorHandler;

        private static readonly string[] _keywords =
        {
            "var",
            "if",
            "then",
            "else",
            "elseif",
            "loop",
            "while",
            "for",
            "foreach",
            "do",
            "end",
            "continue",
            "break",
            "exit",
            "return",
            "in",
            "using",
            "loop",
            "function",
            "new",
        };
        private static readonly string[] _operators =
        {
            "=",
            "+=",
            "-=",
            "/=",
            "*=",
            "++",
            "--",
            "+",
            "-",
            "*",
            "/",
            "%",
            "<",
            "<=",
            ">",
            ">=",
            "==",
            "!=",
            "and",
            "&&",
            "or",
            "||",
            "!",
            "not"
        };
        private static readonly char[] _operatorChars =
        {
            '=',
            '+',
            '-',
            '*',
            '/',
            '%',
            '<',
            '>',
            '&',
            '|',
            '!'
        };
        private char[] _src;
        private int _index;
        private List<ScriptToken> _tokens;

        public ScriptToken[] Tokenize()
        {
            StringBuilder builder = new StringBuilder();
            _index = 0;
            bool inString = false;
            bool inComment = false;
            bool multiLineComment = false;
            bool buildingOperator = false;
            int tokenStart = _index;
            while (_index < _src.Length)
            {
                int charIndex = _index;
                if (!PeekChar().HasValue)
                {
                    break;
                }
                char c = NextChar().Value;
                if (c == '\\')
                {
                    if (!inString)
                    {
                        Error(ScriptErrorCode.T_UnexpectedChar, "Unexpected Character", "\\ outside of string", charIndex);
                        return null;
                    }
                    else if (PeekChar().HasValue)
                    {
                        char next = PeekChar().Value;
                        if (next == '"' || next == '\\')
                        {
                            NextChar();
                            builder.Append(next);
                        }
                        else if (next == 'n')
                        {
                            builder.Append("\r\n");
                        }
                        else
                        {
                            Error(ScriptErrorCode.T_UnexpectedEscape, "Unexpected Escape Sequence", $"Cannot escape {next}", charIndex);
                            return null;
                        }
                    }
                    continue;
                }

                if (inString)
                {
                    builder.Append(c);
                    if (c == '"')
                    {
                        inString = false;
                        _tokens.Add(CreateToken(builder.ToString(), tokenStart));
                        builder.Clear();
                        tokenStart = _index;
                    }
                    continue;
                }
                else if (inComment)
                {
                    if (multiLineComment)
                    {
                        builder.Append(c);
                        if (c == '*' && PeekChar().HasValue && PeekChar().Value == '/')
                        {
                            builder.Append('/');
                            inComment = false;
                            multiLineComment = false;
                            _tokens.Add(CreateToken(builder.ToString(), tokenStart));
                            builder.Clear();
                            NextChar();
                            tokenStart = _index;
                        }
                    }
                    else if (c == '\n')
                    {
                        inComment = false;
                        _tokens.Add(CreateToken(builder.ToString(), tokenStart));
                        builder.Clear();
                        _tokens.Add(CreateToken("\n", charIndex));
                        tokenStart = _index;
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    continue;
                }
                else if (buildingOperator)
                {
                    if (!_operatorChars.Contains(c))
                    {
                        _tokens.Add(CreateToken(builder.ToString(), tokenStart));
                        builder.Clear();
                        buildingOperator = false;
                    }
                    else
                    {
                        builder.Append(c);
                        continue;
                    }
                }

                // We build operators to allow for operators directly
                // next to identifiers (required for casting synax)
                // We make an exception for a forward slash followed
                // by another forward slash or *, as these are comments.
                if (_operatorChars.Contains(c) && (c != '/' || (PeekChar() != '/' && PeekChar() != '*')))
                {
                    if (builder.Length > 0)
                    {
                        _tokens.Add(CreateToken(builder.ToString(), tokenStart));
                        builder.Clear();
                    }

                    builder.Append(c);
                    buildingOperator = true;
                    continue;
                }

                if (char.IsWhiteSpace(c)
                    || c == '['
                    || c == ']'
                    || c == ':')
                {
                    if (builder.Length > 0)
                    {
                        _tokens.Add(CreateToken(builder.ToString(), tokenStart));
                        builder.Clear();
                    }
                    switch (c)
                    {
                        case '\n': _tokens.Add(new ScriptToken("\n", ScriptTokenType.EndLine, charIndex)); break;
                        case '[': _tokens.Add(new ScriptToken("[", ScriptTokenType.OpenBracket, charIndex)); break;
                        case ']': _tokens.Add(new ScriptToken("]", ScriptTokenType.ClosedBracket, charIndex)); break;
                        case ':': _tokens.Add(new ScriptToken(":", ScriptTokenType.Colon, charIndex)); break;
                    }
                    tokenStart = _index;
                    continue;
                }

                builder.Append(c);

                if (c == '#')
                {
                    inComment = true;
                }
                else if (c == '"')
                {
                    inString = true;
                }
                else if (c == '/' && PeekChar().HasValue && PeekChar().Value == '/')
                {
                    inComment = true;
                    builder.Append(NextChar().Value);
                }
                else if (c == '/' && PeekChar().HasValue && PeekChar().Value == '*')
                {
                    inComment = true;
                    multiLineComment = true;
                    builder.Append(NextChar().Value);
                }
            }

            ScriptToken[] tokens = _tokens.ToArray();
            _tokens.Clear();
            _index = 0;
            return tokens;
        }

        private char? PeekChar()
        {
            if (_index >= _src.Length)
            {
                return null;
            }
            return _src[_index];
        }

        private char? NextChar()
        {
            if (_index >= _src.Length)
            {
                return null;
            }
            return _src[_index++];
        }

        private ScriptToken CreateToken(string lexeme, int pos)
        {
            if (lexeme.StartsWith('#') || lexeme.StartsWith("//") || lexeme.StartsWith("/*"))
            {
                return new ScriptToken(lexeme, ScriptTokenType.Comment, pos);
            }
            else if (lexeme.StartsWith('"'))
            {
                return new ScriptToken(lexeme, ScriptTokenType.StringLiteral, pos);
            }
            else if (lexeme == "\n")
            {
                return new ScriptToken(lexeme, ScriptTokenType.EndLine, pos);
            }
            else
            {
                lexeme = lexeme.ToLowerInvariant();
            }

            if (lexeme == "[")
            {
                return new ScriptToken(lexeme, ScriptTokenType.OpenBracket, pos);
            }
            else if (lexeme == "]")
            {
                return new ScriptToken(lexeme, ScriptTokenType.ClosedBracket, pos);
            }
            else if (lexeme == ":")
            {
                return new ScriptToken(lexeme, ScriptTokenType.Colon, pos);
            }
            else if (_keywords.Contains(lexeme))
            {
                return new ScriptToken(lexeme, ScriptTokenType.Keyword, pos);
            }
            else if (lexeme == "null")
            {
                return new ScriptToken(lexeme, ScriptTokenType.NullLiteral, pos);
            }
            else if (lexeme == "true")
            {
                return new ScriptToken(lexeme, ScriptTokenType.TrueLiteral, pos);
            }
            else if (lexeme == "false")
            {
                return new ScriptToken(lexeme, ScriptTokenType.FalseLiteral, pos);
            }
            else if (_operators.Contains(lexeme))
            {
                return new ScriptToken(lexeme, ScriptTokenType.Operator, pos);
            }
            else if (IsNum(lexeme))
            {
                return new ScriptToken(lexeme, ScriptTokenType.NumLiteral, pos);
            }
            else
            {
                if (!IsValidIdentifier(lexeme))
                {
                    Error(ScriptErrorCode.T_InvalidIdentifier, "Invalid Identifier", "Identifiers can only contain alphanumeric characters and underscores.", pos);
                }
                return new ScriptToken(lexeme, ScriptTokenType.Identifier, pos);
            }
        }

        // double.TryParse will return false for very large values
        // Because we don't want very large numbers to become
        // identifiers, we instead test that each char is a digit,
        // underscore, or dot
        private bool IsNum(string str)
        {
            int dots = 0;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c == '-')
                {
                    if (i != 0)
                    {
                        return false;
                    }
                }
                else if (!char.IsDigit(c) && c != '.' && c != '_')
                {
                    return false;
                }
                else if (c == '.')
                {
                    dots++;
                }
            }
            return dots <= 1;
        }

        private bool IsValidIdentifier(string lexeme)
        {
            foreach (char c in lexeme)
            {
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '_')
                {
                    return false;
                }
            }
            return true;
        }

        private void Error(ScriptErrorCode code, string header, string message, int pos)
        {
            ErrorHandler?.Invoke(this, new ScriptCompilerErrorEventArgs(code, header, message, pos, ScriptErrorSeverity.Error));
        }

        public void SetSrc(string src)
        {
            SetSrc((src + "\n").ToCharArray());
        }

        public void SetSrc(char[] src)
        {
            _src = src;
        }

        public ScriptTokenizer()
        {
            _src = null;
            _tokens = new List<ScriptToken>();
            _index = 0;
        }
    }
}
