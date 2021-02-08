using System.IO;
using System.Collections.Generic;

using alm.Other.Structs;

using static alm.Other.Enums.TokenType;
using static alm.Core.Compiler.Compiler.CompilationVariables;

namespace alm.Core.FrontEnd.SyntaxAnalysis
{
    public sealed class Lexer
    {
        private readonly string[] reservedWords = new string[]
        {
            "while",
            "do",
            "for",
            "if",
            "else",
            "not",
            "or",
            "xor",
            "and",
            "func",
            "of",
            "void",
            "char",
            "string",
            "float",
            "boolean",
            "long",
            "integer",
            "import",
            "global",
            "external",
            "true",
            "false",
            "return",
            "continue",
            "break"
        };

        private const int  EOF = -1;
        private const char chEOF = '\0';
        private const char chHTab = '\t';
        private const char chVTab = '\v';
        private const char chNewLn = '\n';
        private const char chWSpace = ' ';
        private const char chCarrRet = '\r';

        private char CurrentChar;

        private int CurrentCharIndex;
        private int CurrentLineIndex;
        private int CurrentTokenIndex;

        private StreamReader Stream;

        public List<Token> Tokens;

        public Token CurrentToken  => Peek(0);
        public Token PreviousToken => Peek(-1);

        public Lexer(string path)
        {
            CurrentCharIndex = 0;
            CurrentLineIndex = 1;
            CurrentTokenIndex = -1;
            Stream = new StreamReader(path, System.Text.Encoding.UTF8);
            GetTokens();
        }
        public Token GetNextToken()
        {
            Token token;
            CurrentTokenIndex++;
            if (CurrentTokenIndex < Tokens.Count)
                token = Tokens[CurrentTokenIndex];
            else
                token = new Token(tkEOF, new Position(CurrentCharIndex, CurrentLineIndex));
            return token;
        }
        public Token Peek(int offset)
        {
            if (CurrentTokenIndex + offset < 0)
                return default;
            else if (CurrentTokenIndex + offset < Tokens.Count) 
                return Tokens[CurrentTokenIndex + offset];
            else 
                return new Token(tkEOF, new Position(CurrentCharIndex, CurrentLineIndex));
        }

        private Token GetReservedWord(string word)
        {
            switch (word)
            {
                case "while": 
                    return new Token(tkWhile, new Position(CurrentCharIndex-5, CurrentLineIndex), word);
                case "do":    
                    return new Token(tkDo,    new Position(CurrentCharIndex-2, CurrentLineIndex), word);
                case "for":
                    return new Token(tkFor, new Position(CurrentCharIndex - 3, CurrentLineIndex), word);
                case "if":    
                    return new Token(tkIf,    new Position(CurrentCharIndex-2, CurrentLineIndex), word);
                case "else":  
                    return new Token(tkElse,  new Position(CurrentCharIndex-4, CurrentLineIndex), word);

                case "not": 
                    return new Token(tkNot, new Position(CurrentCharIndex-3, CurrentLineIndex), word);
                case "or":  
                    return new Token(tkOr,  new Position(CurrentCharIndex-2, CurrentLineIndex), word);
                case "xor":
                    return new Token(tkXor, new Position(CurrentCharIndex - 3, CurrentLineIndex), word);
                case "and": 
                    return new Token(tkAnd, new Position(CurrentCharIndex-3, CurrentLineIndex), word);

                case "func": 
                    return new Token(tkFunc, new Position(CurrentCharIndex-4, CurrentLineIndex),word);

                case "void":    
                    return new Token(tkType, new Position(CurrentCharIndex-4, CurrentLineIndex), word);
                case "char":
                    if (CharForArray())
                        return RecognizeArray(word);
                    return new Token(tkType, new Position(CurrentCharIndex-4, CurrentLineIndex), word);
                case "float":
                    if (CharForArray())
                        return RecognizeArray(word);
                    return new Token(tkType, new Position(CurrentCharIndex-5, CurrentLineIndex), word);
                case "string":
                    if (CharForArray())
                        return RecognizeArray(word);
                    return new Token(tkType, new Position(CurrentCharIndex-6, CurrentLineIndex), word);
                case "boolean":
                    if (CharForArray())
                        return RecognizeArray(word);
                    return new Token(tkType, new Position(CurrentCharIndex-7, CurrentLineIndex), word);
                case "integer":
                    if (CharForArray())
                       return RecognizeArray(word);
                    return new Token(tkType, new Position(CurrentCharIndex-7, CurrentLineIndex), word);
                case "long":
                    if (CharForArray())
                        return RecognizeArray(word);
                    return new Token(tkType, new Position(CurrentCharIndex - 4, CurrentLineIndex), word);

                case "import": 
                    return new Token(tkImport, new Position(CurrentCharIndex-6, CurrentLineIndex), word);

                case "external": 
                    return new Token(tkExternalProp, new Position(CurrentCharIndex-8, CurrentLineIndex));

                case "return": 
                    return new Token(tkRet, new Position(CurrentCharIndex-6, CurrentLineIndex), word);
                case "continue":
                    return new Token(tkContinue, new Position(CurrentCharIndex - 8, CurrentLineIndex), word);
                case "break":
                    return new Token(tkBreak, new Position(CurrentCharIndex - 5, CurrentLineIndex), word);
                case "true":   
                    return new Token(tkBooleanConst, new Position(CurrentCharIndex-4, CurrentLineIndex), word);
                case "false":  
                    return new Token(tkBooleanConst, new Position(CurrentCharIndex-5, CurrentLineIndex), word);

                default: return default;
            }
        }
        private Token GetReservedChar()
        {
            switch (CurrentChar)
            {
                case '=':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkEqual, new Position(CurrentCharIndex, CurrentLineIndex), "==");
                    }
                    return new Token(tkAssign, new Position(CurrentCharIndex, CurrentLineIndex), "=");

                case '!':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkNotEqual, new Position(CurrentCharIndex, CurrentLineIndex), "!=");
                    }
                    return new Token(tkNull);

                case '|':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkBitwiseOrAssign, new Position(CurrentCharIndex, CurrentLineIndex), "|=");
                    }
                    return new Token(tkBitwiseOr, new Position(CurrentCharIndex, CurrentLineIndex), "|");

                case '&':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkBitwiseAndAssign, new Position(CurrentCharIndex, CurrentLineIndex), "&=");
                    }
                    return new Token(tkBitwiseAnd, new Position(CurrentCharIndex, CurrentLineIndex), "&");

                case '^':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkBitwiseXorAssign, new Position(CurrentCharIndex, CurrentLineIndex), "^=");
                    }
                    return new Token(tkBitwiseXor, new Position(CurrentCharIndex, CurrentLineIndex), "^");

                case '<':
                    //<, <= , << , <<=
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkEqualLess, new Position(CurrentCharIndex, CurrentLineIndex), "<=");
                    }

                    if (MatchPeeked('<'))
                    {
                        GetNextChar();
                        if (MatchPeeked('='))
                        {
                            GetNextChar();
                            return new Token(tkLShiftAssign, new Position(CurrentCharIndex, CurrentLineIndex), "<<=");
                        }
                        return new Token(tkLShift, new Position(CurrentCharIndex, CurrentLineIndex), "<<");
                    }
                    return new Token(tkLess, new Position(CurrentCharIndex, CurrentLineIndex), "<");

                case '>':
                    //>, >= , >> , >>=
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkEqualGreater, new Position(CurrentCharIndex, CurrentLineIndex), ">=");
                    }

                    if (MatchPeeked('>'))
                    {
                        GetNextChar();
                        if (MatchPeeked('='))
                        {
                            GetNextChar();
                            return new Token(tkRShiftAssign, new Position(CurrentCharIndex, CurrentLineIndex), ">>=");
                        }
                        return new Token(tkRShift, new Position(CurrentCharIndex,CurrentLineIndex),">>");
                    }
                    return new Token(tkGreater, new Position(CurrentCharIndex, CurrentLineIndex), ">");

                case '+':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkAddAssign, new Position(CurrentCharIndex, CurrentLineIndex), "+=");
                    }
                    return new Token(tkPlus, new Position(CurrentCharIndex, CurrentLineIndex), "+");

                case '-':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkSubAssign, new Position(CurrentCharIndex, CurrentLineIndex), "-=");
                    }
                    return new Token(tkMinus, new Position(CurrentCharIndex, CurrentLineIndex), "-");
                  
                case '*':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkMultAssign, new Position(CurrentCharIndex, CurrentLineIndex), "*=");
                    }
                    if (MatchPeeked('*'))
                    {
                        GetNextChar();
                        if (MatchPeeked('='))
                        {
                            GetNextChar();
                            return new Token(tkPowerAssign, new Position(CurrentCharIndex, CurrentLineIndex), "**=");
                        }
                        return new Token(tkPower, new Position(CurrentCharIndex, CurrentLineIndex), "**");
                    }
                    return new Token(tkMult, new Position(CurrentCharIndex, CurrentLineIndex), "*");

                case '/':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkFDivAssign, new Position(CurrentCharIndex, CurrentLineIndex), "/=");
                    }
                    return new Token(tkFDiv, new Position(CurrentCharIndex, CurrentLineIndex), "/");

                case '%':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkIDivAssign, new Position(CurrentCharIndex, CurrentLineIndex), "%=");
                    }
                    return new Token(tkIDiv, new Position(CurrentCharIndex, CurrentLineIndex), "%");

                case '@':
                    return new Token(tkAt, new Position(CurrentCharIndex, CurrentLineIndex), "@");

                case ';':
                    return new Token(tkSemicolon, new Position(CurrentCharIndex, CurrentLineIndex), ";");

                case ':':
                    return new Token(tkColon, new Position(CurrentCharIndex, CurrentLineIndex), ":");

                case '(':
                    return new Token(tkLpar, new Position(CurrentCharIndex, CurrentLineIndex), "(");

                case ')':
                    return new Token(tkRpar, new Position(CurrentCharIndex, CurrentLineIndex), ")");

                case '[':
                    return new Token(tkSqLbra, new Position(CurrentCharIndex, CurrentLineIndex), "[");

                case ']':
                    return new Token(tkSqRbra, new Position(CurrentCharIndex, CurrentLineIndex), "]");

                case '{':
                    return new Token(tkLbra, new Position(CurrentCharIndex, CurrentLineIndex), "{");

                case '}':
                    return new Token(tkRbra, new Position(CurrentCharIndex, CurrentLineIndex), "}");

                case '"':
                    return new Token(tkDQuote, new Position(CurrentCharIndex, CurrentLineIndex), "\"");

                case '\'':
                    return new Token(tkSQuote, new Position(CurrentCharIndex, CurrentLineIndex), "\'");

                case ',':
                    return new Token(tkComma, new Position(CurrentCharIndex, CurrentLineIndex), ",");

                default:
                    return new Token(tkNull);
            }
        }
        private Token RecognizeIdentifier()
        {
            int line = CurrentLineIndex;
            int start = CurrentCharIndex;
            string ident = string.Empty;

            while (CharForIdentifier() && line == CurrentLineIndex)
            {
                ident += CurrentChar.ToString();
                GetNextChar();
            }
            if (IsWordReserved(ident)) 
                return GetReservedWord(ident);
            return new Token(tkIdentifier, new Position(start, line), ident);
        }
        private Token RecognizeNumber()
        {
            int start = CurrentCharIndex;
            string num = string.Empty;
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
                    num += CurrentChar.ToString();
                GetNextChar();
            }
            if (dot)
                return new Token(tkRealConst, new Position(start, CurrentLineIndex), num);
            else
                return new Token(tkIntConst, new Position(start, CurrentLineIndex), num);
        }
        private Token RecognizeArray(string type)
        {
            bool failure = false;

            int dimensions = 1;
            string typeString = type;
            int startLine = CurrentLineIndex;

            //skip '['
            GetNextChar();

            while (!Match(']'))
            {
                if (!Match(',') || Match(chEOF) || startLine != CurrentLineIndex)
                {
                    failure = true;
                    break;
                }
                if (Match(','))
                    dimensions++;
                GetNextChar();
            }

            if (!failure)
                //if error doesn't occurred, skip ']'
                GetNextChar();
            else
                return new Token(tkType, new Position(CurrentCharIndex - typeString.Length, CurrentLineIndex),typeString);

            //creates type of array base on it dimension
            typeString += '[';
            for (int i = 0; i < dimensions-1; i++)
                typeString += ',';
            typeString += ']';

            return new Token(tkType, new Position(CurrentCharIndex - typeString.Length, CurrentLineIndex), typeString);
        }
        private Token[] RecognizeString()
        {
            //also returns quote tokens
            List<Token> tokens = new List<Token>();
            string str = string.Empty;
            int start = CurrentCharIndex;
            int line = CurrentLineIndex;

            if (CharIsDQuote())
                tokens.Add(new Token(tkDQuote, new Position(CurrentCharIndex, line),"\""));
            GetNextChar();
            while (CharForString(line))
            {
                CheckForEscapeChar();
                str += CurrentChar.ToString();
                GetNextChar();
            }
            tokens.Add(new Token(tkStringConst, new Position(start+1, line), str));

            if (CharIsDQuote())
            {
                tokens.Add(new Token(tkDQuote, new Position(CurrentCharIndex, line),"\""));
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
                tokens.Add(new Token(tkSQuote, new Position(CurrentCharIndex, CurrentLineIndex), "\'"));
            GetNextChar();
            if (CharForSChar())
            {
                CheckForEscapeChar();
                tokens.Add(new Token(tkCharConst, new Position(CurrentCharIndex,CurrentLineIndex), CurrentChar.ToString()));
                GetNextChar();
            }
            if (CharIsSQuote())
            {
                tokens.Add(new Token(tkSQuote, new Position(CurrentCharIndex, CurrentLineIndex), "\'"));
                GetNextChar();
            }

            return tokens.ToArray();
        }
        private void GetNextChar()
        {
            if (Stream.Peek() == EOF)
                CurrentChar = chEOF;
            else
            {
                CurrentChar = (char)Stream.Read();
                switch(CurrentChar)
                {
                    case '\n':
                        CurrentLineIndex++;
                        GetNextChar();
                        break;

                    case '\r':
                        CurrentCharIndex = 0;
                        GetNextChar();
                        break;
                        
                    case '\t':
                    case '\v':
                        CurrentCharIndex++;
                        GetNextChar();
                        break;

                    case '/':
                        switch ((char)Stream.Peek())
                        {
                            case '*':
                                MultiLineCommentary();
                                break;
                            case '/':
                                SingleLineCommentary();
                                break;
                            default:
                                //default case like in main "switch"
                                CurrentCharIndex++;
                                break;
                        }
                        break;

                    default:
                        CurrentCharIndex++;
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
                    GetNextChar();
                else
                {
                    Tokens.Add(GetReservedChar());
                    GetNextChar();
                }
            }
            Stream.Close();
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
            //skip start '/' char
            GetNextChar();

            int startLine = CurrentLineIndex;
            while (startLine == CurrentLineIndex &&
                   !Match(chEOF))
                GetNextChar();
        }
        private bool CharForNumber()
        {
            if (Match(chEOF))
                return false;
            return (CharIsDigit(CurrentChar) ||
                    CharIsDot()) ? true : false;
        }
        private bool CharForIdentifier(bool fsym = false)
        {
            if (Match(chEOF))
                return false;
            if (CharIsDigit(CurrentChar) && 
                !fsym)
                    return true;
            if (CharIsLetter(CurrentChar) || 
                CharIsUnderscore())
                return true;
            return false;
        }
        private bool CharForString(int linePos)
        {
            if (Match(chEOF))
                return false;
            if (Match('\"'))
                return false;
            if (this.CurrentLineIndex != linePos)
                return false;
            return true;
        }
        private bool CharForSChar()
        {
            if (Match(chEOF))
                return false;
            return true;
        }
        private bool CharForArray()
        {
            return Match('[') ? true : false;
        }
        private bool Match(char ch)
        {
            return CurrentChar == ch ? true : false;
        }
        private bool MatchPeeked(char ch)
        {
            if (Stream == null)
                return false;
            return (char)Stream.Peek() == ch ? true : false;
        }
        private bool IsWordReserved(string word)
        {
            for (int i = 0; i < reservedWords.Length; i++)
                if (reservedWords[i] == word)
                    return true;
            return false;
        }

        private char GetEscapeChar(char ch)
        {
            switch (ch)
            {
                case '\'':
                    return '\'';
                case '\"':
                    return '\"';
                case '\\':
                    return '\\';
                case 'n':
                    return '\n';
                case 't':
                    return '\t';
                case 'b':
                    return '\b';
                case 'f':
                    return '\f';
                case 'v':
                    return '\v';
                case '0':
                    return '\0';

                default:
                    return ch;
            }
        }
        private void CheckForEscapeChar()
        {
            if (Match('\\'))
            {
                switch ((char)Stream.Peek())
                {
                    case '\'':
                    case '"':
                    case '\\':
                    case 'n':
                    case 'r':
                    case 't':
                    case 'b':
                    case 'f':
                    case 'v':
                    case '0':
                        CurrentChar = GetEscapeChar((char)Stream.Peek());
                        Stream.Read();
                        CurrentCharIndex++;
                        break;
                }
            }
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
        private bool CharIsAt()  => CharsAreSame(CurrentChar, '@') ? true : false;
        private bool CharIsDot() => CharsAreSame(CurrentChar, '.') ? true : false;
        private bool CharIsUnderscore() => CharsAreSame(CurrentChar, '_') ? true : false;
        private bool CharIsDQuote() => CharsAreSame(CurrentChar, '\"') ? true : false;
        private bool CharIsSQuote() => CharsAreSame(CurrentChar, '\'') ? true : false;
        private bool CharIsWSpace() => CharsAreSame(CurrentChar, ' ') ? true : false;
    }
}