using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

public class PropertyAccessItemComparer : IEqualityComparer<PropertyAccessItem>
{
    public bool Equals(PropertyAccessItem x, PropertyAccessItem y)
    {
        if (x.PropertySymbols.Count != y.PropertySymbols.Count) return false;

        return !x.PropertySymbols.Where((t, i) => !t.Equals(y.PropertySymbols[i], SymbolEqualityComparer.Default))
            .Any();
    }

    public int GetHashCode(PropertyAccessItem obj)
    {
        return 0;
    }
}

public readonly struct PropertyAccessItem(ExpressionSyntax expression, SemanticModel model)
{
    private readonly Lazy<IReadOnlyList<IPropertySymbol>> _list = new(() => GetPropertySymbols(expression, model));

    public ExpressionSyntax Expression { get; } = expression;

    public string InitName => "Init_" + string.Join("_", PropertySymbols.Select(i => i.Name.ToString()));

    public IReadOnlyList<IPropertySymbol> PropertySymbols => _list.Value;

    private static IReadOnlyList<IPropertySymbol> GetPropertySymbols(ExpressionSyntax expression, SemanticModel model)
    {
        var symbol = model.GetSymbolInfo(expression).Symbol as IPropertySymbol;
        if (expression is MemberAccessExpressionSyntax member)
        {
            return symbol is null
                ? GetPropertySymbols(member.Expression, model)
                : [..GetPropertySymbols(member.Expression, model), symbol];
        }

        return symbol is null ? [] : [symbol];
    }

    public bool HasSymbol(IPropertySymbol symbol)
        => PropertySymbols.Any(s => s.Equals(symbol, SymbolEqualityComparer.Default));

    public StatementSyntax InvokeInit() => ExpressionStatement(InvocationExpression(IdentifierName(InitName)));

    public LocalFunctionStatementSyntax CreateInit()
    {
        return LocalFunctionStatement(
                PredefinedType(
                    Token(SyntaxKind.VoidKeyword)),
                Identifier(InitName))
            .WithBody(
                Block(CreateStatements()));
    }

    private IReadOnlyList<StatementSyntax> CreateStatements()
    {
        //TODO: make it work!
        var names = string.Join(", ", PropertySymbols.Select(i => i.Name.ToString()));

        return
        [
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(names),
                    LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        Literal(1))))
        ];
    }
}