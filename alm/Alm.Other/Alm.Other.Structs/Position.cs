namespace alm.Other.Structs
{
    public struct Position
    {
        public int CharIndex { get; set; }
        public int LineIndex { get; set; }

        public Position(int Start, int Line)
        {
            this.CharIndex  = Start;
            this.LineIndex  = Line;
        }

        public Position(Token Token)
        {
            this.CharIndex  = Token.Context.StartsAt.CharIndex;
            this.LineIndex  = Token.Context.StartsAt.LineIndex;
        }
        public override string ToString() => $"(Строка: {LineIndex} Позиция: {CharIndex})";
    }
}
