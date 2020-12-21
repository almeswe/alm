namespace alm.Other.Structs
{
    public struct Position
    {
        public int Start { get; set; }
        public int End   { get; set; }
        public int Line  { get; set; }

        public Position(int Start, int End, int Line)
        {
            this.Start = Start;
            this.End   = End;
            this.Line  = Line;
        }

        public Position(Token Token)
        {
            this.Start = Token.Context.StartsAt.Start;
            this.End   = Token.Context.EndsAt.End;
            this.Line  = Token.Context.StartsAt.Line;
        }
        public override string ToString() => $"(Строка: {Line} Позиция: {Start})";
    }
}
