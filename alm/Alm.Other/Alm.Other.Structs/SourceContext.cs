using alm.Core.SyntaxAnalysis;
using static alm.Core.Compiler.Compiler;

namespace alm.Other.Structs
{
    public struct SourceContext
    {
        public Position StartsAt { get; set; }
        public Position EndsAt   { get; set; }

        public string FilePath { get; set; }

        public SourceContext(Position StartsAt, Position EndsAt)
        {
            this.StartsAt = StartsAt;
            this.EndsAt   = EndsAt;
            this.FilePath = CurrentParsingFile;
        }

        public static SourceContext GetSourceContext(Token Token) => 
            new SourceContext(new Position(Token.Context.StartsAt.CharIndex, Token.Context.StartsAt.LineIndex), new Position(Token.Context.EndsAt.CharIndex, Token.Context.EndsAt.LineIndex));
        public static SourceContext GetSourceContext(Token sToken, Token fToken)  => new SourceContext(new Position(sToken.Context.StartsAt.CharIndex, sToken.Context.StartsAt.LineIndex), new Position(fToken.Context.EndsAt.CharIndex, fToken.Context.EndsAt.LineIndex));
        public static SourceContext GetSourceContext(SyntaxTreeNode node)         => new SourceContext(node.SourceContext.StartsAt, node.SourceContext.EndsAt);
        public static SourceContext GetSourceContext(SyntaxTreeNode lnode, SyntaxTreeNode rnode) => new SourceContext(lnode.SourceContext.StartsAt, rnode.SourceContext.EndsAt);

        public override string ToString() => $"От {StartsAt} До {EndsAt}";

        public static bool operator ==(SourceContext op1, SourceContext op2)
        {
            return op1.Equals(op2);
        }

        public static bool operator !=(SourceContext op1, SourceContext op2)
        {
            return !op1.Equals(op2);
        }
    }
}