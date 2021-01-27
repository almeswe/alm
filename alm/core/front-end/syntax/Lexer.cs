using System.IO;
using System.Collections.Generic;

using alm.Other.Structs;

using static alm.Other.Enums.TokenType;

namespace alm.Core.FrontEnd.SyntaxAnalysis
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
        }
        public Token GetNextToken()
        {
            Token token;
            currentTokenIndex++;
            if (currentTokenIndex < Tokens.Count)
                token = Tokens[currentTokenIndex];
            else
                token = new Token(tkEOF, new Position(currentCharIndex, currentLineIndex));
            return token;
        }
        public Token Peek(int offset)
        {
            if (currentTokenIndex + offset < 0)
                return default;
            else if (currentTokenIndex + offset < Tokens.Count) 
                return Tokens[currentTokenIndex + offset];
            else 
                return new Token(tkEOF, new Position(currentCharIndex, currentLineIndex));
        }

        private Token GetReservedWord(string word)
        {
            switch (word)
            {
                case "while": 
                    return new Token(tkWhile, new Position(currentCharIndex-5, currentLineIndex), word);
                case "do":    
                    return new Token(tkDo,    new Position(currentCharIndex-2, currentLineIndex), word);
                case "if":    
                    return new Token(tkIf,    new Position(currentCharIndex-2, currentLineIndex), word);
                case "else":  
                    return new Token(tkElse,  new Position(currentCharIndex-4, currentLineIndex), word);

                case "not": 
                    return new Token(tkNot, new Position(currentCharIndex-3, currentLineIndex), word);
                case "or":  
                    return new Token(tkOr,  new Position(currentCharIndex-2, currentLineIndex), word);
                case "and": 
                    return new Token(tkAnd, new Position(currentCharIndex-3, currentLineIndex), word);

                case "func": 
                    return new Token(tkFunc, new Position(currentCharIndex-4, currentLineIndex),word);

                case "void":    
                    return new Token(tkType, new Position(currentCharIndex-4, currentLineIndex), word);
                case "char":
                    if (CharForArray())
                        return RecognizeArray(word);
                    return new Token(tkType, new Position(currentCharIndex-4, currentLineIndex), word);
                case "float":
                    if (CharForArray())
                        return RecognizeArray(word);
                    return new Token(tkType, new Position(currentCharIndex-5, currentLineIndex), word);
                case "string":
                    if (CharForArray())
                        return RecognizeArray(word);
                    return new Token(tkType, new Position(currentCharIndex-6, currentLineIndex), word);
                case "boolean":
                    if (CharForArray())
                        return RecognizeArray(word);
                    return new Token(tkType, new Position(currentCharIndex-7, currentLineIndex), word);
                case "integer":
                    if (CharForArray())
                       return RecognizeArray(word);
                    return new Token(tkType, new Position(currentCharIndex-7, currentLineIndex), word);

                case "import": 
                    return new Token(tkImport, new Position(currentCharIndex-6, currentLineIndex), word);

                case "external": 
                    return new Token(tkExternalProp, new Position(currentCharIndex-8, currentLineIndex));

                case "return": 
                    return new Token(tkRet, new Position(currentCharIndex-6, currentLineIndex), word);
                case "true":   
                    return new Token(tkBooleanConst, new Position(currentCharIndex-4, currentLineIndex), word);
                case "false":  
                    return new Token(tkBooleanConst, new Position(currentCharIndex-5, currentLineIndex), word);

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
                        return new Token(tkEqual, new Position(currentCharIndex, currentLineIndex), "==");
                    }
                    return new Token(tkAssign, new Position(currentCharIndex, currentLineIndex), "=");

                case '!':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkNotEqual, new Position(currentCharIndex, currentLineIndex), "!=");
                    }
                    return new Token(tkNull);

                //rename to IDiv
                case '%':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkFDivAssign, new Position(currentCharIndex, currentLineIndex), "%=");
                    }
                    return new Token(tkFDiv, new Position(currentCharIndex, currentLineIndex), "%");

                case '|':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkBitwiseOrAssign, new Position(currentCharIndex, currentLineIndex), "|=");
                    }
                    return new Token(tkBitwiseOr, new Position(currentCharIndex, currentLineIndex), "|");

                case '&':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkBitwiseAndAssign, new Position(currentCharIndex, currentLineIndex), "&=");
                    }
                    return new Token(tkBitwiseAnd, new Position(currentCharIndex, currentLineIndex), "&");

                case '^':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkBitwiseXorAssign, new Position(currentCharIndex, currentLineIndex), "^=");
                    }
                    return new Token(tkBitwiseXor, new Position(currentCharIndex, currentLineIndex), "^");

                case '<':
                    //<, <= , << , <<=
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkEqualLess, new Position(currentCharIndex, currentLineIndex), "<=");
                    }

                    if (MatchPeeked('<'))
                    {
                        GetNextChar();
                        if (MatchPeeked('='))
                        {
                            GetNextChar();
                            return new Token(tkLShiftAssign, new Position(currentCharIndex, currentLineIndex), "<<=");
                        }
                        return new Token(tkLShift, new Position(currentCharIndex, currentLineIndex), "<<");
                    }
                    return new Token(tkLess, new Position(currentCharIndex, currentLineIndex), "<");

                case '>':
                    //>, >= , >> , >>=
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkEqualGreater, new Position(currentCharIndex, currentLineIndex), ">=");
                    }

                    if (MatchPeeked('>'))
                    {
                        GetNextChar();
                        if (MatchPeeked('='))
                        {
                            GetNextChar();
                            return new Token(tkRShiftAssign, new Position(currentCharIndex, currentLineIndex), ">>=");
                        }
                        return new Token(tkRShift, new Position(currentCharIndex,currentLineIndex),">>");
                    }
                    return new Token(tkGreater, new Position(currentCharIndex, currentLineIndex), ">");

                case '+':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkAddAssign, new Position(currentCharIndex, currentLineIndex), "+=");
                    }
                    return new Token(tkPlus, new Position(currentCharIndex, currentLineIndex), "+");

                case '-':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkSubAssign, new Position(currentCharIndex, currentLineIndex), "-=");
                    }
                    return new Token(tkMinus, new Position(currentCharIndex, currentLineIndex), "-");
                  
                case '*':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkMultAssign, new Position(currentCharIndex, currentLineIndex), "*=");
                    }
                    return new Token(tkMult, new Position(currentCharIndex, currentLineIndex), "*");

                //rename to FDiv
                case '/':
                    if (MatchPeeked('='))
                    {
                        GetNextChar();
                        return new Token(tkIDivAssign, new Position(currentCharIndex, currentLineIndex), "/=");
                    }
                    return new Token(tkIDiv, new Position(currentCharIndex, currentLineIndex), "/");

                case '@':
                    return new Token(tkAt, new Position(currentCharIndex, currentLineIndex), "@");

                case ';':
                    return new Token(tkSemicolon, new Position(currentCharIndex, currentLineIndex), ";");

                case ':':
                    return new Token(tkColon, new Position(currentCharIndex, currentLineIndex), ":");

                case '(':
                    return new Token(tkLpar, new Position(currentCharIndex, currentLineIndex), "(");

                case ')':
                    return new Token(tkRpar, new Position(currentCharIndex, currentLineIndex), ")");

                case '[':
                    return new Token(tkSqLbra, new Position(currentCharIndex, currentLineIndex), "[");

                case ']':
                    return new Token(tkSqRbra, new Position(currentCharIndex, currentLineIndex), "]");

                case '{':
                    return new Token(tkLbra, new Position(currentCharIndex, currentLineIndex), "{");

                case '}':
                    return new Token(tkRbra, new Position(currentCharIndex, currentLineIndex), "}");

                case '"':
                    return new Token(tkDQuote, new Position(currentCharIndex, currentLineIndex), "\"");

                case '\'':
                    return new Token(tkSQuote, new Position(currentCharIndex, currentLineIndex), "\'");

                case ',':
                    return new Token(tkComma, new Position(currentCharIndex, currentLineIndex), ",");

                default:
                    return new Token(tkNull);
            }
        }
        private Token RecognizeIdentifier()
        {
            int line = currentLineIndex;
            int start = currentCharIndex;
            string ident = string.Empty;

            while (CharForIdentifier() && line == currentLineIndex)
            {
                ident += currentChar.ToString();
                GetNextChar();
                //!!!!!!!!!!!!!!!!!!!!!!!
                //здесь появляется \n ,обнуляет позицию и добавляет строку
                //потом в функции GetReservedWord создается контексит по имеющимся данным
            }
            if (IsWordReserved(ident)) 
                return GetReservedWord(ident);
            return new Token(tkId, new Position(start, line), ident);
        }
        private Token RecognizeNumber()
        {
            int start = currentCharIndex;
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
                    num += currentChar.ToString();
                GetNextChar();
            }
            if (dot)
                return new Token(tkFloatConst, new Position(start, currentLineIndex), num);
            else
                return new Token(tkIntConst, new Position(start, currentLineIndex), num);
        }
        private Token RecognizeArray(string type)
        {
            //расширить до возможности определять многомерные массивы

            bool failure = false;

            int dimensions = 1;
            string typeString = type;
            int startLine = currentLineIndex;

            //skip '['
            GetNextChar();

            while (!Match(']'))
            {
                if (!Match(',') || Match(chEOF) || startLine != currentLineIndex)
                {
                    failure = true;
                    break;
                }
                if (Match(','))
                    dimensions++;
                GetNextChar();
            }

            if (!failure)
                //если не встречено ошибок, то просто пропускаем ']'
                GetNextChar();
            else
                return new Token(tkType, new Position(currentCharIndex - typeString.Length, currentLineIndex),typeString);

            //создаем тип массива на основе размерности
            typeString += '[';
            for (int i = 0; i < dimensions-1; i++)
                typeString += ',';
            typeString += ']';

            return new Token(tkType, new Position(currentCharIndex - typeString.Length, currentLineIndex), typeString);
        }
        private Token[] RecognizeString()
        {
            //also returns quote tokens
            List<Token> tokens = new List<Token>();
            string str = string.Empty;
            int start = currentCharIndex;
            int line = currentLineIndex;

            if (CharIsDQuote())
                tokens.Add(new Token(tkDQuote, new Position(currentCharIndex, line),"\""));
            GetNextChar();
            while (CharForString(line))
            {
                CheckForEscapeChar();
                str += currentChar.ToString();
                GetNextChar();
            }
            tokens.Add(new Token(tkStringConst, new Position(start+1, line), str));

            if (CharIsDQuote())
            {
                tokens.Add(new Token(tkDQuote, new Position(currentCharIndex, line),"\""));
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
                tokens.Add(new Token(tkSQuote, new Position(currentCharIndex, currentLineIndex), "\'"));
            GetNextChar();
            if (CharForSChar())
            {
                CheckForEscapeChar();
                tokens.Add(new Token(tkCharConst, new Position(currentCharIndex,currentLineIndex), currentChar.ToString()));
                GetNextChar();
            }
            if (CharIsSQuote())
            {
                tokens.Add(new Token(tkSQuote, new Position(currentCharIndex, currentLineIndex), "\'"));
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
                        currentLineIndex++;
                        GetNextChar();
                        break;

                    case '\r':
                        currentCharIndex = 0;
                        GetNextChar();
                        break;
                        
                    case '\t':
                    case '\v':
                        currentCharIndex++;
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
                    GetNextChar();
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
            //skip start '/' char
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
        private bool CharForString(int linePos)
        {
            if (Match(chEOF))
                return false;
            if (Match('\"'))
                return false;
            if (this.currentLineIndex != linePos)
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
                switch ((char)stream.Peek())
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
                        currentChar = GetEscapeChar((char)stream.Peek());
                        stream.Read();
                        currentCharIndex++;
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
        private bool CharIsAt()  => CharsAreSame(currentChar, '@') ? true : false;
        private bool CharIsDot() => CharsAreSame(currentChar, '.') ? true : false;
        private bool CharIsUnderscore() => CharsAreSame(currentChar, '_') ? true : false;
        private bool CharIsDQuote() => CharsAreSame(currentChar, '\"') ? true : false;
        private bool CharIsSQuote() => CharsAreSame(currentChar, '\'') ? true : false;
        private bool CharIsWSpace() => CharsAreSame(currentChar, ' ') ? true : false;
    }
}