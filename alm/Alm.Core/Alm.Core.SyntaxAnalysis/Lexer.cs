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

        private int currTokenIndex;
        private int currCharIndex;
        private TextReader reader;

        private bool tokensCreated = false;

        private Token[] _tokens;

        public string Path { get; private set; }
        public Token CurrentToken => Peek(0);
        public Token PreviousToken => Peek(-1);

        public Lexer(string path)
        {
            this.Path = path;
            reader = new StreamReader(path);
            currCharIndex = -1;
            currTokenIndex = -1;
            charPos = 0;
            linePos = 1;
        }

        public Token GetNextToken()
        {
            Token token;
            currTokenIndex++;
            if (tokensCreated)
            {
                if (currTokenIndex < _tokens.Length)
                {
                    token = _tokens[currTokenIndex];
                    return token;
                }
                else
                {
                    token = new Token(tkEOF, new Position(charPos, charPos, linePos));
                    return token;
                }
            }
            else
            {
                token = Token.GetNullToken();
                return token;
            }
        }
        public Token Peek(int offset)
        {
            if (currTokenIndex + offset < _tokens.Length) return _tokens[currTokenIndex + offset];
            else if (currTokenIndex + offset < 0) return new Token(tkNull);
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
                else if (char.IsLetter(currentChar)) tokens.Add(RecognizeIdent());
                else if (char.IsWhiteSpace(currentChar)) GetNextChar();
                else { tokens.Add(RecognizeSymbol()); GetNextChar(); }
            }
            reader.Close();
            if (!tokensCreated) tokensCreated = true;
            _tokens = tokens.ToArray();
            return _tokens;
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
                currCharIndex++;
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
            while (char.IsDigit(currentChar) || char.IsLetter(currentChar) || currentChar == 95)
            {
                ident += currentChar.ToString();
                currCharIndex++;
                GetNextChar();
            }
            int end = charPos;
            if (!Token.IsNull(GetReservedExpr(ident))) return GetReservedExpr(ident);
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
                currCharIndex++;
                GetNextChar();
            }
            return new Token(tkStringConst, new Position(start, start+str.Length, line), str);
        }

        private Token RecognizeSymbol()
        {
            switch (currentChar)
            {
                case '=':
                    if (reader.Peek() == '=') { GetNextChar(); return new Token(tkEqual, new Position(charPos, charPos+2, linePos)); }
                    return new Token(tkAssign, new Position(charPos, charPos+1, linePos));

                case '!':
                    if (reader.Peek() == '=') { GetNextChar(); return new Token(tkNotEqual, new Position(charPos, charPos+2, linePos)); }
                    return new Token(tkNull);

                case ';': return new Token(tkSemicolon, new Position(charPos, charPos + 1, linePos));
                case ':': return new Token(tkColon,     new Position(charPos, charPos + 1, linePos));
                case '(': return new Token(tkLpar,      new Position(charPos, charPos + 1, linePos));
                case ')': return new Token(tkRpar,      new Position(charPos, charPos + 1, linePos));
                case '+': return new Token(tkPlus,      new Position(charPos, charPos + 1, linePos));
                case '-': return new Token(tkMinus,     new Position(charPos, charPos + 1, linePos));
                case '"': return new Token(tkQuote,     new Position(charPos, charPos + 1, linePos));

                case '<':
                    if (reader.Peek() == '=') { GetNextChar(); return new Token(tkEqualLess, new Position(charPos, charPos+2, linePos)); }
                    return new Token(tkLess, new Position(charPos, charPos+1, linePos));

                case '>':
                    if (reader.Peek() == '=') { GetNextChar(); return new Token(tkEqualGreater, new Position(charPos, charPos+2, linePos)); }
                    return new Token(tkGreater, new Position(charPos, charPos+1, linePos));

                case '{': return new Token(tkLbra,  new Position(charPos, charPos + 1, linePos));
                case '}': return new Token(tkRbra,  new Position(charPos, charPos + 1, linePos));
                case '*': return new Token(tkMult,  new Position(charPos, charPos + 1, linePos));
                case '/': return new Token(tkDiv,   new Position(charPos, charPos + 1, linePos));
                case ',': return new Token(tkComma, new Position(charPos, charPos + 1, linePos));
                case chEOF: return new Token(tkEOF, new Position(charPos, charPos + 1, linePos));
                default: return new Token(tkNull);
            }
        }
        private Token GetReservedExpr(string expr)
        {
            switch (expr)
            {
                case "while": return new Token(tkWhile, new Position(charPos - 5, charPos, linePos));
                case "do":    return new Token(tkDo,    new Position(charPos - 2, charPos, linePos));
                case "if":    return new Token(tkIf,    new Position(charPos - 2, charPos, linePos));
                case "else":  return new Token(tkElse,  new Position(charPos - 4, charPos, linePos));

                case "not": return new Token(tkNot, new Position(charPos - 3, charPos, linePos));
                case "or":  return new Token(tkOr,  new Position(charPos - 2, charPos, linePos));
                case "and": return new Token(tkAnd, new Position(charPos - 3, charPos, linePos));

                case "function": return new Token(tkFunc, new Position(charPos - 8, charPos, linePos));
                case "of":       return new Token(tkOf,   new Position(charPos - 2, charPos, linePos));

                case "integer": return new Token(tkType, new Position(charPos - 7, charPos, linePos), "integer");
                case "boolean": return new Token(tkType, new Position(charPos - 7, charPos, linePos), "boolean");
                case "string":  return new Token(tkType, new Position(charPos - 6, charPos, linePos), "string");
                case "float" :  return new Token(tkType, new Position(charPos - 5, charPos, linePos), "float");

                case "import": return new Token(tkImport, new Position(charPos - 6, charPos, linePos));
                case "global": return new Token(tkGlobal, new Position(charPos - 6, charPos, linePos));

                case "return": return new Token(tkRet,   new Position(charPos - 6, charPos, linePos));
                case "true":   return new Token(tkBooleanConst,  new Position(charPos - 4, charPos, linePos), "true");
                case "false":  return new Token(tkBooleanConst,  new Position(charPos - 5, charPos, linePos), "false");
                default: return Token.GetNullToken();
            }
        }
        private void GetNextChar()
        {
            if (reader.Peek() == EOF)
            {
                currentChar = chEOF;
                return;
            }
            else
            {
                currentChar = (char)reader.Read();
                //Комментарий
                CheckCommentary();
                //
                currCharIndex++;
                charPos++;
                if (currentChar == chNewLn)
                {
                    charPos = 0;
                    linePos++;
                    GetNextChar();
                }
            }
        }

        private void CheckCommentary()
        {
            int line = linePos;
            if (currentChar == '#')
                while (currentChar == chEOF || linePos == line)
                    GetNextChar();
        }
    }
}