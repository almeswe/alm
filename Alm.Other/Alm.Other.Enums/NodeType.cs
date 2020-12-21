namespace alm.Other.Enums
{
    public enum NodeType
    {
        FOR,
        Program,
        EMPTY,
        Error,

        If,
        IfElse,
        While,
        Do,
        Variable,
        IntegerConstant,
        BooleanConstant,
        StringConstant,
        Type,
        PrimaryNode,

        Function,
        FunctionCall,
        Argument,

        Import,

        LessThan,
        MoreThan,
        EqualLess,
        EqualMore,
        Equal,
        NotEqual,

        Assignment,
        Declaration,
        Division,
        Multiplication,
        Addition,
        Substraction,
        And,
        Or,
        //Xor,
        Not,

        True,
        False,

        //special
        Body, ElseBody, Condition,Arguments,

        Return,
        BinaryExpression,
        BooleanExpression
    };
}
