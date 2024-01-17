#nullable disable

namespace Reqnroll.VisualStudio.Discovery.TagExpressions;

public class TagExpressionParser
{
    private const char ESCAPING_CHAR = '\\';

    private static readonly IDictionary<string, Assoc> ASSOC = new Dictionary<string, Assoc>
    {
        {"or", Assoc.LEFT},
        {"and", Assoc.LEFT},
        {"not", Assoc.RIGHT}
    };

    private static readonly IDictionary<string, int> PREC = new Dictionary<string, int>
    {
        {"(", -2},
        {")", -1},
        {"or", 0},
        {"and", 1},
        {"not", 2}
    };

    public virtual ITagExpression Parse(string infix)
    {
        var tokens = Tokenize(infix);
        if (!tokens.Any()) return new True();

        var operators = new Stack<string>();
        var expressions = new Stack<ITagExpression>();
        TokenType expectedTokenType = TokenType.OPERAND;
        foreach (string token in tokens)
            if (IsUnary(token))
            {
                Check(expectedTokenType, TokenType.OPERAND);
                operators.Push(token);
                expectedTokenType = TokenType.OPERAND;
            }
            else if (IsBinary(token))
            {
                Check(expectedTokenType, TokenType.OPERATOR);
                while (operators.Count > 0 && IsOperator(operators.Peek()) && (
                           ASSOC[token] == Assoc.LEFT && PREC[token] <= PREC[operators.Peek()]
                           ||
                           ASSOC[token] == Assoc.RIGHT && PREC[token] < PREC[operators.Peek()])
                      )
                    PushExpr(Pop(operators), expressions);
                operators.Push(token);
                expectedTokenType = TokenType.OPERAND;
            }
            else if ("(".Equals(token))
            {
                Check(expectedTokenType, TokenType.OPERAND);
                operators.Push(token);
                expectedTokenType = TokenType.OPERAND;
            }
            else if (")".Equals(token))
            {
                Check(expectedTokenType, TokenType.OPERATOR);
                while (operators.Count > 0 && !"(".Equals(operators.Peek())) PushExpr(Pop(operators), expressions);
                if (operators.Count == 0) throw new TagExpressionException("Syntax error. Unmatched )");
                if ("(".Equals(operators.Peek())) Pop(operators);
                expectedTokenType = TokenType.OPERATOR;
            }
            else
            {
                Check(expectedTokenType, TokenType.OPERAND);
                PushExpr(token, expressions);
                expectedTokenType = TokenType.OPERATOR;
            }

        while (operators.Count > 0)
        {
            if ("(".Equals(operators.Peek())) throw new TagExpressionException("Syntax error. Unmatched (");
            PushExpr(Pop(operators), expressions);
        }

        return expressions.Pop();
    }

    private static List<string> Tokenize(string expr)
    {
        var tokens = new List<string>();

        bool isEscaped = false;
        StringBuilder token = null;
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (ESCAPING_CHAR == c)
            {
                isEscaped = true;
            }
            else
            {
                if (char.IsWhiteSpace(c))
                {
                    // skip
                    if (null != token)
                    {
                        // end of token
                        tokens.Add(token.ToString());
                        token = null;
                    }
                }
                else
                {
                    switch (c)
                    {
                        case '(':
                        case ')':
                            if (!isEscaped)
                            {
                                if (null != token)
                                {
                                    // end of token
                                    tokens.Add(token.ToString());
                                    token = null;
                                }

                                tokens.Add(c.ToString());
                            }

                            break;
                        default:
                            if (null == token)
                                // start of token
                                token = new StringBuilder();
                            token.Append(c);
                            break;
                    }
                }

                isEscaped = false;
            }
        }

        if (null != token)
            // end of token
            tokens.Add(token.ToString());
        return tokens;
    }

    private void Check(TokenType expectedTokenType, TokenType tokenType)
    {
        if (expectedTokenType != tokenType)
            throw new TagExpressionException(
                $"Syntax error. Expected {expectedTokenType.ToString().ToLowerInvariant()}");
    }

    private T Pop<T>(Stack<T> stack)
    {
        if (!stack.Any()) throw new TagExpressionException("empty stack");
        return stack.Pop();
    }

    private void PushExpr(string token, Stack<ITagExpression> stack)
    {
        switch (token)
        {
            case "and":
                ITagExpression rightAndExpr = Pop(stack);
                stack.Push(new And(Pop(stack), rightAndExpr));
                break;
            case "or":
                ITagExpression rightOrExpr = Pop(stack);
                stack.Push(new Or(Pop(stack), rightOrExpr));
                break;
            case "not":
                stack.Push(new Not(Pop(stack)));
                break;
            default:
                stack.Push(new Literal(token));
                break;
        }
    }

    private bool IsUnary(string token) => "not".Equals(token);

    private bool IsBinary(string token) => "or".Equals(token) || "and".Equals(token);

    private bool IsOperator(string token) => ASSOC.ContainsKey(token);

    public static ITagExpression CreateTagLiteral(string tag) => new Literal(tag);

    private static string[] EnsureArray(IEnumerable<string> variables)
    {
        if (variables is string[] variablesArray)
            return variablesArray;
        return variables.ToArray();
    }

    private enum TokenType
    {
        OPERAND,
        OPERATOR
    }

    private enum Assoc
    {
        LEFT,
        RIGHT
    }

    private class Literal : ITagExpression
    {
        private readonly string value;

        public Literal(string value)
        {
            this.value = value;
        }

        public bool Evaluate(IEnumerable<string> variables) => variables.Contains(value);

        public override string ToString() => value.Replace("\\(", "\\\\(").Replace("\\)", "\\\\)");
    }

    private class Or : ITagExpression
    {
        private readonly ITagExpression left;
        private readonly ITagExpression right;

        public Or(ITagExpression left, ITagExpression right)
        {
            this.left = left;
            this.right = right;
        }

        public bool Evaluate(IEnumerable<string> variables)
        {
            var variablesArray = EnsureArray(variables);
            return left.Evaluate(variablesArray) || right.Evaluate(variablesArray);
        }

        public override string ToString() => "( " + left + " or " + right + " )";
    }

    private class And : ITagExpression
    {
        private readonly ITagExpression left;
        private readonly ITagExpression right;

        public And(ITagExpression left, ITagExpression right)
        {
            this.left = left;
            this.right = right;
        }

        public bool Evaluate(IEnumerable<string> variables)
        {
            var variablesArray = EnsureArray(variables);
            return left.Evaluate(variablesArray) && right.Evaluate(variablesArray);
        }

        public override string ToString() => "( " + left + " and " + right + " )";
    }

    private class Not : ITagExpression
    {
        private readonly ITagExpression expr;

        public Not(ITagExpression expr)
        {
            this.expr = expr;
        }

        public bool Evaluate(IEnumerable<string> variables) => !expr.Evaluate(variables);

        public override string ToString() => "not ( " + expr + " )";
    }

    private class True : ITagExpression
    {
        public bool Evaluate(IEnumerable<string> variables) => true;
    }
}
