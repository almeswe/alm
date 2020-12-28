using alm.Other.Enums;

namespace alm.Other.Structs
{
    public struct Token
    {
        public string Value          { get; private set; }
        public TokenType TokenType   { get; private set; }
        public SourceContext Context { get; private set; }

        public Token(TokenType TokenType, string Value = null)
        {
            this.Value     = Value;
            this.TokenType = TokenType;
            this.Context  = new SourceContext();
        }
        public Token(TokenType TokenType, SourceContext Context, string Value = null)
        {
            this.Value     = Value;
            this.TokenType = TokenType;
            this.Context   = Context;
        }
        public Token(TokenType TokenType, Position Position, string Value = null)
        {
            this.Value     = Value;
            this.TokenType = TokenType;
            this.Context   = new SourceContext(new Position(Position.Start,Position.Start,Position.Line),new Position(Position.End,Position.End,Position.Line));
        }
        public static Token GetNullToken() => new Token(TokenType.tkNull);
        public static bool IsNull(Token Token)
        {
            if (Token.TokenType == TokenType.tkNull) return true;
            return false;
        }
        public string ToExtendedString() => $"{this.TokenType}:{this.Value}[{this.Context.StartsAt};{this.Context.EndsAt}][{this.Context.StartsAt.Line}]";
        public override string ToString()
        {
            if (Value != null) return $"{this.TokenType}:{this.Value}";
            return $"{this.TokenType}";
        }
    }
}
