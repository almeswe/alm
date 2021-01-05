using System.IO;
using System.Collections.Generic;

using alm.Other.Structs;

using static alm.Other.Enums.TokenType;

namespace alm.Core.SyntaxAnalysis
{
    public sealed class Lexer
    {
        private readonly string[] reservedWords = new string[]
        {
            "while",
            "do",
            "if",
            "else",
            "not",
            "or",
            "and",
            "func",
            "of",
            "void",
            "char",
            "string",
            "float",
            "boolean",
            "integer",
            "import",
            "global",
            "external",
            "true",
            "false",
            "return"
        };

        private const int  EOF = -1;
        private const char chEOF = '\0';
        private const char chHTab = '\t';
        private const char chVTab = '\v';
        private const char chNewLn = '\n';
        private const char chWSpace = ' ';
        private const char chCarrRet = '\r';

        private char currentChar;

        private int currentCharIndex;
        private int currentLineIndex;
        private int currentTokenIndex;

        private StreamReader stream;

        public List<Token> Tokens;

        public Token CurrentToken  => Peek(0);
        public Token PreviousToken => Peek(-1);

        public Lexer(string path)
        {
            stream = new StreamReader(path,System.Text.Encoding.UTF8);
            currentCharIndex = 0;
            currentLineIndex = 1;
            currentTokenIndex = -1;
            GetTokens();
            /*foreach(var t in Tokens)
            {
                System.Console.WriteLine(t.ToExtendedString());
            }*/
        }
        public Token GetNextToken()
        {
            Token token;
            currentTokenIndex++;
            if (currentTokenIndex < Tokens.Count)
                token = Tokens[currentTokenIndex];
            else
                token = new Token(tkEOF, new Position(currentCharIndex, currentCharIndex, currentLineIndex));
            return token;
        }
        public Token Peek(int offset)
        {
            if (currentTokenIndex + offset < Tokens.Count) 
                return Tokens[currentTokenIndex + offset];
            else if (currentTokenIndex + offset < 0) 
                return default;
            else 
                return new Token(tkEOF, new Position(currentCharIndex, currentCharIndex, currentLineIndex));
        }
        private Token GetReservedWord(string word)
        {
            switch (word)
            {
                case "while": 
                    return new Token(tkWhile, new Position(currentCharIndex - 5, currentCharIndex, currentLineIndex));
                case "do":    
                    return new Token(tkDo,    new Position(currentCharIndex - 2, currentCharIndex, currentLineIndex));
                case "if":    
                    return new Token(tkIf,    new Position(currentCharIndex - 2, currentCharIndex, currentLineIndex));
                case "else":  
                    return new Token(tkElse,  new Position(currentCharIndex - 4, currentCharIndex, currentLineIndex));

                case "not": 
                    return new Token(tkNot, new Position(currentCharIndex - 3, currentCharIndex, currentLineIndex));
                case "or":  
                    return new Token(tkOr,  new Position(currentCharIndex - 2, currentCharIndex, currentLineIndex));
                case "and": 
                    return new Token(tkAnd, new Position(currentCharIndex - 3, currentCharIndex, currentLineIndex));

                case "func": 
                    return new Token(tkFunc, new Position(currentCharIndex - 4, currentCharIndex, currentLineIndex));
                case "of":   
                    return new Token(tkOf,   new Position(currentCharIndex - 2, currentCharIndex, currentLineIndex));

                case "void":    
                    return new Token(tkType, new Position(currentCharIndex - 4, currentCharIndex, currentLineIndex), "void");
                case "char":    
                    return new Token(tkType, new Position(currentCharIndex - 4, currentCharIndex, currentLineIndex), "char");
                case "float":   
                    return new Token(tkType, new Position(currentCharIndex - 5, currentCharIndex, currentLineIndex), "float");
                case "string":  
                    return new Token(tkType, new Position(currentCharIndex - 6, currentCharIndex, currentLineIndex), "string");
                case "boolean": 
                    return new Token(tkType, new Position(currentCharIndex - 7, currentCharIndex, currentLineIndex), "boolean");
                case "integer": 
                    return new Token(tkType, new Position(currentCharIndex - 7, currentCharIndex, currentLineIndex), "integer");

                case "import": 
                    return new Token(tkImport, new Position(currentCharIndex - 6, currentCharIndex, currentLineIndex));
                case "global": 
                    return new Token(tkGlobal, new Position(currentCharIndex - 6, currentCharIndex, currentLineIndex));

                case "external": 
                    return new Token(tkExternalProp, new Position(currentCharIndex - 8, currentCharIndex, currentLineIndex));

                case "return": 
                    return new Token(tkRet,          new Position(currentCharIndex - 6, currentCharIndex, currentLineIndex));
                case "true":   
                    return new Token(tkBooleanConst, new Position(currentCharIndex - 4, currentCharIndex, currentLineIndex), "true");
                case "false":  
                    return new Token(tkBooleanConst, new Position(currentCharIndex - 5, currentCharIndex, currentLineIndex), "false");

                default: return default;
            }
        }
        private Token GetReservedChar()
        {
            switch (currentChar)
            {
                case '=':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkEqual, new Position(currentCharIndex, currentCharIndex + 2, currentLineIndex));
                    }
                    return new Token(tkAssign, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));

                case '!':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkNotEqual, new Position(currentCharIndex, currentCharIndex + 2, currentLineIndex));
                    }
                    return new Token(tkNull);

                case '<':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkEqualLess, new Position(currentCharIndex, currentCharIndex + 2, currentLineIndex));
                    }
                    return new Token(tkLess, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));

                case '>':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkEqualGreater, new Position(currentCharIndex, currentCharIndex + 2, currentLineIndex));
                    }
                    return new Token(tkGreater, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));

                case '+':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkAddAssign, new Position(currentCharIndex, currentCharIndex + 2, currentLineIndex));
                    }
                    return new Token(tkPlus, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));

                case '-':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkSubAssign, new Position(currentCharIndex, currentCharIndex + 2, currentLineIndex));
                    }
                    return new Token(tkMinus, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));

                case '*':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkMultAssign, new Position(currentCharIndex, currentCharIndex + 2, currentLineIndex));
                    }
                    return new Token(tkMult, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));

                case '/':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkDivAssign, new Position(currentCharIndex, currentCharIndex + 2, currentLineIndex));
                    }
                    return new Token(tkDiv, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));

                case '@':
                    return new Token(tkAt, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));
                case ';':
                    return new Token(tkSemicolon, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));
                case ':':
                    return new Token(tkColon, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));
                case '(':
                    return new Token(tkLpar, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));
                case ')':
                    return new Token(tkRpar, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));

                case '{':
                    return new Token(tkLbra, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));
                case '}':
                    return new Token(tkRbra, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));
                case '"':
                    return new Token(tkDQuote, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));
                case '\'':
                    return new Token(tkDQuote, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));
                case ',':
                    return new Token(tkComma, new Position(currentCharIndex, currentCharIndex + 1, currentLineIndex));
                default:
                    return new Token(tkNull);
            }
        }
        private Token RecognizeIdentifier()
        {
            string ident = string.Empty;
            int start = currentCharIndex;
            while (CharForIdentifier())
            {
                ident += currentChar.ToString();
                GetNextChar();
            }
            int end = currentCharIndex;
            if (IsWordReserved(ident)) 
                return GetReservedWord(ident);
            return new Token(tkId, new Position(start, start+ident.Length, currentLineIndex), ident);
        }
        private Token RecognizeNumber()
        {
            string num = string.Empty;
            int start = currentCharIndex;
            bool dot = false;
            while (CharForNumber())
            {
                if (CharIsDot())
                {
                    if (dot) break;
                    else dot = true;
                    num += ',';
                }
                else
                    num += currentChar.ToString();
                GetNextChar();
            }
            int end = currentCharIndex;
            if (dot)
                return new Token(tkFloatConst, new Position(start, end, currentLineIndex), num);
            else
                return new Token(tkIntConst, new Position(start, end, currentLineIndex), num);
        }
        private Token[] RecognizeString()
        {
            //also returns quote tokens
            List<Token> tokens = new List<Token>();
            string str = string.Empty;

            int line = currentLineIndex;
            int start = currentCharIndex;

            if (CharIsDQuote())
                tokens.Add(new Token(tkDQuote, new Position(start, start+1, line)));
            GetNextChar();
            while (CharForString(line))
            {
                str += currentChar.ToString();
                GetNextChar();
            }
            int end = currentCharIndex;
            tokens.Add(new Token(tkStringConst, new Position(currentCharIndex,end, line),str));

            if (CharIsDQuote())
            {
                tokens.Add(new Token(tkDQuote, new Position(currentCharIndex, currentCharIndex + 1, line)));
                //skip quote
                GetNextChar();
            }

            return tokens.ToArray();
        }
        private Token[] RecognizeChar()
        {
            //also returns quote tokens
            List<Token> tokens = new List<Token>();

            if (CharIsSQuote())
                tokens.Add(new Token(tkSQuote, new Position(currentCharIndex, currentCharIndex, currentLineIndex)));
            GetNextChar();
            tokens.Add(new Token(tkCharConst, new Position(currentCharIndex, currentCharIndex, currentLineIndex), currentChar.ToString()));
            GetNextChar();
            if (CharIsSQuote())
            {
                tokens.Add(new Token(tkSQuote, new Position(currentCharIndex, currentCharIndex, currentLineIndex)));
                GetNextChar();
            }

            return tokens.ToArray();
        }
        private void GetNextChar()
        {
            if (stream.Peek() == EOF)
                currentChar = chEOF;
            else
            {
                currentChar = (char)stream.Read();
                switch(currentChar)
                {
                    case '\n':
                        currentCharIndex = 1;
                        currentLineIndex++;
                        GetNextChar();
                        break;

                    case '\r':
                        currentCharIndex = 1;
                        GetNextChar();
                        break;
                        
                    case '\t':
                    case '\v':
                        GetNextChar();
                        break;

                    case '/':
                        switch ((char)stream.Peek())
                        {
                            case '*':
                                MultiLineCommentary();
                                break;
                            case '/':
                                SingleLineCommentary();
                                break;
                            default:
                                //default case like in main "switch"
                                currentCharIndex++;
                                break;
                        }
                        break;

                    default:
                        currentCharIndex++;
                        break;
                }
            }
        }
        private void GetTokens()
        {
            Tokens = new List<Token>();
            GetNextChar();
            while (!Match(chEOF))
            {
                if (CharIsDQuote())
                    Tokens.AddRange(RecognizeString());
                else if (CharIsSQuote())
                    Tokens.AddRange(RecognizeChar());
                else if (CharForNumber())
                    Tokens.Add(RecognizeNumber());
                else if (CharForIdentifier(true))
                    Tokens.Add(RecognizeIdentifier());
                else if (CharIsWSpace())
                {
                    currentCharIndex++;
                    GetNextChar();
                }
                else
                {
                    Tokens.Add(GetReservedChar());
                    GetNextChar();
                }
            }
            stream.Close();
        }
        private void MultiLineCommentary()
        {
            //skip start '*' char
            GetNextChar();
            while (!(Match('*') && MatchPeeked('/')) &&
                   !Match(chEOF))
                GetNextChar();

            //skip '*','/' chars
            GetNextChar();
            GetNextChar();
        }
        private void SingleLineCommentary()
        {
            //skip start '\' char
            GetNextChar();

            int startLine = currentLineIndex;
            while (startLine == currentLineIndex &&
                   !Match(chEOF))
                GetNextChar();
        }
        private bool CharForNumber()
        {
            if (Match(chEOF))
                return false;
            return (CharIsDigit(currentChar) ||
                    CharIsDot()) ? true : false;
        }
        private bool CharForIdentifier(bool fsym = false)
        {
            if (Match(chEOF))
                return false;
            if (CharIsDigit(currentChar) && 
                !fsym)
                    return true;
            if (CharIsLetter(currentChar) || 
                CharIsUnderscore())
                return true;
            return false;
        }
        private bool CharForString(int linePos, bool fq = false)
        {
            if (Match(chEOF))
                return false;
            if (Match('"') && !fq)
                return false;
            if (this.currentLineIndex != linePos)
                return false;
            return true;
        }
        private bool Match(char ch)
        {
            return currentChar == ch ? true : false;
        }
        private bool MatchPeeked(char ch)
        {
            if (stream == null)
                return false;
            return (char)stream.Peek() == ch ? true : false;
        }
        private bool IsWordReserved(string word)
        {
            for (int i = 0; i < reservedWords.Length; i++)
                if (reservedWords[i] == word)
                    return true;
            return false;
        }
        private bool CharIsDigit(char ch) => 
            ch >= 48 && ch <= 57 
            ? true : false;
        private bool CharIsLetter(char ch) => 
            //encoding UTF-8
            ((ch >= 97 && ch <= 122) || (ch >= 65 && ch <= 90)) ||    // eng
            ((ch >= 1072 && ch <= 1103) || (ch >= 1040 && ch <= 1071))// rus
            ? true : false;
        private bool CharIsLetterOrDigit(char ch) => CharIsDigit(ch) || CharIsLetter(ch) ? true : false;
        private bool CharsAreSame(char ch, char sch) => ch == sch ? true : false;
        private bool CharIsAt()  => CharsAreSame(currentChar, '@') ? true : false;
        private bool CharIsDot() => CharsAreSame(currentChar, '.') ? true : false;
        private bool CharIsUnderscore() => CharsAreSame(currentChar, '_') ? true : false;
        private bool CharIsDQuote() => CharsAreSame(currentChar, '\"') ? true : false;
        private bool CharIsSQuote() => CharsAreSame(currentChar, '\'') ? true : false;
        private bool CharIsWSpace() => CharsAreSame(currentChar, ' ') ? true : false;
    }
}