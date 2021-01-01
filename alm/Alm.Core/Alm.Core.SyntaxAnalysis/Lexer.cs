using System.IO;
using System.Linq;
using System.Collections.Generic;

using alm.Other.Structs;

using static alm.Other.Enums.TokenType;

namespace alm.Core.SyntaxAnalysis
{
    internal sealed class Lexer
    {
        private const int  EOF = -1;
        private const char chEOF = '\0';
        private const char chSpace = ' ';
        private const char chNewLn = '\n';
        private const char chCarrRet = '\r';

        private int charPos;
        private int linePos;

        public char currentChar;

        private int currentTokenIndex;
        private int currentCharIndex;
        private TextReader reader;

        private bool tokensCreated = false;

        private Token[] tokens;

        public string Path { get; private set; }
        public Token CurrentToken => Peek(0);
        public Token PreviousToken => Peek(-1);

        public Lexer(string path)
        {
            this.Path = path;
            reader = new StreamReader(path);
            currentCharIndex = -1;
            currentTokenIndex = -1;
            charPos = 0;
            linePos = 1;
        }

        public Token GetNextToken()
        {
            Token token;
            currentTokenIndex++;
            if (tokensCreated)
            {
                if (currentTokenIndex < tokens.Length)
                    token = tokens[currentTokenIndex];
                else
                    token = new Token(tkEOF, new Position(charPos, charPos, linePos));
                return token;
            }
            else
            {
                token = Token.GetNullToken();
                return token;
            }
        }
        public Token Peek(int offset)
        {
            if (currentTokenIndex + offset < tokens.Length) return tokens[currentTokenIndex + offset];
            else if (currentTokenIndex + offset < 0) return new Token(tkNull);
            else return new Token(tkEOF, new Position(charPos, charPos, linePos));
        }
        public Token[] GetTokens()
        {
            IList<Token> tokens = new List<Token>();
            GetNextChar();
            while (currentChar != chEOF)
            {
                if (currentChar == '"')
                {
                    GetNextChar();
                    tokens.Add(new Token(tkQuote, new Position(charPos-1, charPos+1, linePos)));
                    tokens.Add(RecognizeString());
                    if (currentChar == '"')
                    {
                        tokens.Add(new Token(tkQuote, new Position(charPos-1, charPos, linePos)));
                        GetNextChar();
                    }
                }
                else if (char.IsDigit(currentChar)) tokens.Add(RecognizeConst());
                else if (char.IsLetter(currentChar) || currentChar == 64) tokens.Add(RecognizeIdent());
                else if (char.IsWhiteSpace(currentChar)) GetNextChar();
                else { tokens.Add(RecognizeSymbol()); GetNextChar(); }
            }
            reader.Close();
            if (!tokensCreated) tokensCreated = true;
            this.tokens = tokens.ToArray();
            return this.tokens;
        }
        private Token RecognizeConst()
        {
            string num = string.Empty;
            int start = charPos;
            bool dot  = false;
            while (char.IsDigit(currentChar) || currentChar == 46)
            {
                if (currentChar == 46)
                {
                    if (dot) break;
                    else dot = true;
                    num += ',';
                }
                else
                    num += currentChar.ToString();
                currentCharIndex++;
                GetNextChar();
            }
            int end = charPos;
            if (dot)
                return new Token(tkFloatConst, new Position(start, end, linePos), num);
            return new Token(tkIntConst, new Position(start, end, linePos), num);
        }
        private Token RecognizeIdent()
        {
            string ident = string.Empty;
            int start = charPos;
            while (char.IsDigit(currentChar) || char.IsLetter(currentChar) || currentChar == 95 || currentChar == 64)
            {
                ident += currentChar.ToString();
                currentCharIndex++;
                GetNextChar();
            }
            int end = charPos;
            if (!Token.IsNull(GetReservedWord(ident))) return GetReservedWord(ident);
            return new Token(tkId, new Position(start, end, linePos), ident);
        }

        private Token RecognizeString()
        {
            string str = string.Empty;
            int line = this.linePos;
            int start = charPos;
            while (currentChar != 34 && currentChar != chEOF)
            {
                if (line != this.linePos) break;
                str += currentChar.ToString();
                currentCharIndex++;
                GetNextChar();
            }
            return new Token(tkStringConst, new Position(start, start+str.Length, line), str);
        }

        private Token RecognizeSymbol()
        {
            switch (currentChar)
            {
                case '=':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkEqual, new Position(charPos, charPos+2, linePos));
                    }
                    return new Token(tkAssign, new Position(charPos, charPos+1, linePos));

                case '!':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkNotEqual, new Position(charPos, charPos+2, linePos));
                    }
                    return new Token(tkNull);

                case '<':
                    if (reader.Peek() == '=')
                    { 
                        GetNextChar();
                        return new Token(tkEqualLess, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkLess, new Position(charPos, charPos + 1, linePos));

                case '>':
                    if (reader.Peek() == '=') 
                    {
                        GetNextChar(); 
                        return new Token(tkEqualGreater, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkGreater, new Position(charPos, charPos + 1, linePos));

                case '+':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkAddAssign, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkPlus,  new Position(charPos, charPos + 1, linePos));

                case '-':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkSubAssign, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkMinus, new Position(charPos, charPos + 1, linePos));

                case '*':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkMultAssign, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkMult,  new Position(charPos, charPos + 1, linePos));

                case '/':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkDivAssign, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkDiv,   new Position(charPos, charPos + 1, linePos));

                case ';': return new Token(tkSemicolon, new Position(charPos, charPos + 1, linePos));
                case ':': return new Token(tkColon,     new Position(charPos, charPos + 1, linePos));
                case '(': return new Token(tkLpar,      new Position(charPos, charPos + 1, linePos));
                case ')': return new Token(tkRpar,      new Position(charPos, charPos + 1, linePos));

                case '{': return new Token(tkLbra,  new Position(charPos, charPos + 1, linePos));
                case '}': return new Token(tkRbra,  new Position(charPos, charPos + 1, linePos));
                case '"': return new Token(tkQuote, new Position(charPos, charPos + 1, linePos));
                case ',': return new Token(tkComma, new Position(charPos, charPos + 1, linePos));
                case chEOF: return new Token(tkEOF, new Position(charPos, charPos + 1, linePos));
                default: return new Token(tkNull);
            }
        }
        private Token GetReservedWord(string word)
        {
            switch (word)
            {
                case "while": return new Token(tkWhile, new Position(charPos - 5, charPos, linePos));
                case "do":    return new Token(tkDo,    new Position(charPos - 2, charPos, linePos));
                case "if":    return new Token(tkIf,    new Position(charPos - 2, charPos, linePos));
                case "else":  return new Token(tkElse,  new Position(charPos - 4, charPos, linePos));

                case "not": return new Token(tkNot, new Position(charPos - 3, charPos, linePos));
                case "or":  return new Token(tkOr,  new Position(charPos - 2, charPos, linePos));
                case "and": return new Token(tkAnd, new Position(charPos - 3, charPos, linePos));

                case "func": return new Token(tkFunc, new Position(charPos - 4, charPos, linePos));
                case "of":   return new Token(tkOf,   new Position(charPos - 2, charPos, linePos));

                case "void":    return new Token(tkType, new Position(charPos - 4, charPos, linePos), "void");
                case "float":   return new Token(tkType, new Position(charPos - 5, charPos, linePos), "float");
                case "string":  return new Token(tkType, new Position(charPos - 6, charPos, linePos), "string");
                case "boolean": return new Token(tkType, new Position(charPos - 7, charPos, linePos), "boolean");
                case "integer": return new Token(tkType, new Position(charPos - 7, charPos, linePos), "integer");

                case "import": return new Token(tkImport, new Position(charPos - 6, charPos, linePos));
                case "global": return new Token(tkGlobal, new Position(charPos - 6, charPos, linePos));

                case "@external": return new Token(tkExternalProp, new Position(charPos - 9, charPos, linePos));

                case "return": return new Token(tkRet,          new Position(charPos - 6, charPos, linePos));
                case "true":   return new Token(tkBooleanConst, new Position(charPos - 4, charPos, linePos), "true");
                case "false":  return new Token(tkBooleanConst, new Position(charPos - 5, charPos, linePos), "false");

                default: return Token.GetNullToken();
            }
        }
        private void GetNextChar()
        {
            if (reader.Peek() == EOF)
                currentChar = chEOF;
            else
            {
                currentChar = (char)reader.Read();
                charPos++;
                currentCharIndex++;
                if (currentChar == chNewLn)
                {
                    charPos = 0;
                    linePos++;
                    GetNextChar();
                }
            }
            CheckCommentary();
        }

        private void CheckCommentary()
        {
            int line = linePos;
            if (currentChar == '/')
            {
                //Проверка на однострочный комментарий
                if (reader.Peek() == '/')
                {
                    reader.Read();
                    while (linePos == line)
                        GetNextChar();
                }
                //Проверка на многострочный комментарий
                if (reader.Peek() == '*')
                {
                    reader.Read();
                    while (currentChar != '*' || reader.Peek() != '/')
                        GetNextChar();
                    GetNextChar();
                    GetNextChar();
                }
            }

        }
    }
}