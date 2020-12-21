﻿using System.IO;
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
            ParsingFile.Path = path;
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
                    token = new Token(tkEOF, new Position(charPos, charPos, linePos), ParsingFile.Path);
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
            else return new Token(tkEOF, new Position(charPos, charPos, linePos), ParsingFile.Path);
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
                    tokens.Add(new Token(tkQuote, new Position(charPos-1, charPos+1, linePos), ParsingFile.Path));
                    tokens.Add(RecognizeString());
                    if (currentChar == '"')
                    {
                        tokens.Add(new Token(tkQuote, new Position(charPos-1, charPos, linePos), ParsingFile.Path));
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
            while (char.IsDigit(currentChar))
            {
                num += currentChar.ToString();
                currCharIndex++;
                GetNextChar();
            }
            int end = charPos;
            return new Token(tkNum, new Position(start, end, linePos), ParsingFile.Path, num);
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
            return new Token(tkId, new Position(start, end, linePos), ParsingFile.Path, ident);
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
            return new Token(tkString, new Position(start, start+str.Length, line), ParsingFile.Path, str);
        }

        private Token RecognizeSymbol()
        {
            switch (currentChar)
            {
                case '=':
                    if (reader.Peek() == '=') { GetNextChar(); return new Token(tkEqual, new Position(charPos, charPos+2, linePos), ParsingFile.Path); }
                    return new Token(tkAssign, new Position(charPos, charPos+1, linePos), ParsingFile.Path);

                case '!':
                    if (reader.Peek() == '=') { GetNextChar(); return new Token(tkNotEqual, new Position(charPos, charPos+2, linePos), ParsingFile.Path); }
                    return new Token(tkNull);

                case ';': return new Token(tkSemicolon, new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case ':': return new Token(tkColon,     new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case '(': return new Token(tkLpar,      new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case ')': return new Token(tkRpar,      new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case '+': return new Token(tkPlus,      new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case '-': return new Token(tkMinus,     new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case '"': return new Token(tkQuote,     new Position(charPos, charPos + 1, linePos), ParsingFile.Path);

                case '<':
                    if (reader.Peek() == '=') { GetNextChar(); return new Token(tkEqualLess, new Position(charPos, charPos+2, linePos), ParsingFile.Path); }
                    return new Token(tkLess, new Position(charPos, charPos+1, linePos), ParsingFile.Path);

                case '>':
                    if (reader.Peek() == '=') { GetNextChar(); return new Token(tkEqualMore, new Position(charPos, charPos+2, linePos), ParsingFile.Path); }
                    return new Token(tkMore, new Position(charPos, charPos+1, linePos), ParsingFile.Path);

                case '{': return new Token(tkLbra,  new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case '}': return new Token(tkRbra,  new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case '*': return new Token(tkMult,  new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case '/': return new Token(tkDiv,   new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case ',': return new Token(tkComma, new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                case chEOF: return new Token(tkEOF, new Position(charPos, charPos + 1, linePos), ParsingFile.Path);
                default: return new Token(tkNull);
            }
        }
        private Token GetReservedExpr(string expr)
        {
            switch (expr)
            {
                case "while": return new Token(tkWhile, new Position(charPos - 5, charPos, linePos), ParsingFile.Path);
                case "do":    return new Token(tkDo,    new Position(charPos - 2, charPos, linePos), ParsingFile.Path);
                case "if":    return new Token(tkIf,    new Position(charPos - 2, charPos, linePos), ParsingFile.Path);
                case "else":  return new Token(tkElse,  new Position(charPos - 4, charPos, linePos), ParsingFile.Path);

                case "not": return new Token(tkNot, new Position(charPos - 3, charPos, linePos), ParsingFile.Path);
                case "or":  return new Token(tkOr,  new Position(charPos - 2, charPos, linePos), ParsingFile.Path);
                case "and": return new Token(tkAnd, new Position(charPos - 3, charPos, linePos), ParsingFile.Path);

                case "function": return new Token(tkFunc, new Position(charPos - 8, charPos, linePos), ParsingFile.Path);
                case "of":       return new Token(tkOf,   new Position(charPos - 2, charPos, linePos), ParsingFile.Path);

                case "integer": return new Token(tkType, new Position(charPos - 7, charPos, linePos), ParsingFile.Path, "integer");
                case "boolean": return new Token(tkType, new Position(charPos - 7, charPos, linePos), ParsingFile.Path, "boolean");
                case "string":  return new Token(tkType, new Position(charPos - 6, charPos, linePos), ParsingFile.Path, "string");

                case "import": return new Token(tkImport, new Position(charPos - 6, charPos, linePos), ParsingFile.Path);

                case "return": return new Token(tkRet,   new Position(charPos - 6, charPos, linePos), ParsingFile.Path);
                case "true":   return new Token(tkTrue,  new Position(charPos - 4, charPos, linePos), ParsingFile.Path, "true");
                case "false":  return new Token(tkFalse, new Position(charPos - 5, charPos, linePos), ParsingFile.Path, "false");
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
    }
}