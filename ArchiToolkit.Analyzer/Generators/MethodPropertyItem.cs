using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

public class MethodPropertyItem(PropertyDeclarationSyntax node, IPropertySymbol symbol, SemanticModel model)
    : BasePropertyDependencyItem(node, symbol)
{
    public override IReadOnlyList<MemberDeclarationSyntax> GetMembers() =>
        [..base.GetMembers(), GetLazyField(), GetMethod(), ClearMethod(), InitMethod()];

    protected override AccessorDeclarationSyntax UpdateAccess(AccessorDeclarationSyntax accessor)
    {
        if (accessor.Kind() is not SyntaxKind.GetAccessorDeclaration) return accessor;
        return accessor.WithExpressionBody(
            ArrowExpressionClause(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(Name.LazyName),
                    IdentifierName("Value"))));
    }

    private FieldDeclarationSyntax GetLazyField()
    {
        return FieldDeclaration(
                VariableDeclaration(
                        GenericName(
                                Identifier("global::System.Lazy"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(TypeName)))))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(Name.LazyName)))))
            .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword)));
    }

    private MethodDeclarationSyntax GetMethod()
    {
        return MethodDeclaration(
                IdentifierName(TypeName),
                Identifier(Name.GetName))
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                IdentifierName("global::ArchiToolkit.Const"))))))
            .WithModifiers(
                TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.PartialKeyword)))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax ClearMethod()
    {
        return MethodDeclaration(
                PredefinedType(
                    Token(SyntaxKind.VoidKeyword)),
                Identifier(Name.ClearName))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword)))
            .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
            .WithBody(
                Block(
                    IfStatement(
                        IsPatternExpression(
                            IdentifierName(Name.LazyName),
                            UnaryPattern(
                                ConstantPattern(
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression)))),
                        Block(
                            IfStatement(
                                PrefixUnaryExpression(
                                    SyntaxKind.LogicalNotExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(Name.LazyName),
                                        IdentifierName("IsValueCreated"))),
                                ReturnStatement()),
                            ExpressionStatement(
                                ConditionalAccessExpression(
                                    IdentifierName(Name.NameChanging),
                                    InvocationExpression(
                                        MemberBindingExpression(
                                            IdentifierName("Invoke"))))))),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(Name.LazyName),
                            ImplicitObjectCreationExpression()
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                IdentifierName(Name.GetName))))))),
                    ExpressionStatement(
                        ConditionalAccessExpression(
                            IdentifierName(Name.NameChanged),
                            InvocationExpression(
                                MemberBindingExpression(
                                    IdentifierName("Invoke")))))));
    }

    private MethodDeclarationSyntax InitMethod()
    {
        StatementSyntax[] items =
        [
            ExpressionStatement(
                InvocationExpression(
                    IdentifierName(Name.ClearName))),
            ..GetStatementsForInit()
        ];

        return MethodDeclaration(
                PredefinedType(
                    Token(SyntaxKind.VoidKeyword)),
                Identifier(Name.InitName))
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
            .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
            .WithBody(Block(items));
    }

    private IEnumerable<StatementSyntax> GetStatementsForInit()
    {
        var accessors = GetAccessItems()
            .Where(i => !i.HasSymbol(Symbol) && i.ValidPropertySymbols.Any()).ToImmutableArray();

        return
        [
            ..accessors.Select(exp => exp.InvokeInit()),
            ReturnStatement(),
            ..accessors.Select(exp => exp.CreateInit(Name.ClearName)),
        ];
    }

    internal IEnumerable<PropertyAccessItem> GetAccessItems()
    {
        return GetExpressionBody().Select(exp => new PropertyAccessItem(exp, model))
            .ToImmutableHashSet(new PropertyAccessItemComparer());

        IReadOnlyList<ExpressionSyntax> GetExpressionBody()
        {
            var method = GetMethodDeclaration();
            if (method is null) return [];

            List<MethodDeclarationSyntax> methods = [method];
            IEnumerable<ExpressionSyntax> result = [];

            while (methods.Any())
            {
                List<MethodDeclarationSyntax> next = [];
                foreach (var m in methods)
                {
                    result = result.Concat(GetExpressions(m, out var invocations));
                
                    next.AddRange(invocations.Select(invocation => model.GetSymbolInfo(invocation).Symbol)
                        .OfType<ISymbol>()
                        .Where(s => s.ContainingSymbol.Equals(Symbol.ContainingSymbol, SymbolEqualityComparer.Default))
                        .Select(s => GetMethodDeclaration(s.Name))
                        .OfType<MethodDeclarationSyntax>());
                }
                methods = next;
            }
            
            return result.ToImmutableArray();
        }
    }
    
    private static IReadOnlyList<ExpressionSyntax> GetExpressions(MethodDeclarationSyntax method, out InvocationExpressionSyntax[] invocations)
    {
        var body = method.Body as SyntaxNode ?? method.ExpressionBody;
        if (body is null)
        {
            invocations = [];
            return [];
        }

        invocations = body.GetChildren<InvocationExpressionSyntax>().ToArray();

        //TODO: maybe I lost sth. Please NO.
        var baseMembers = body.GetChildren<AssignmentExpressionSyntax>().OfType<ExpressionSyntax>()
            .Concat(body.GetChildren<BinaryExpressionSyntax>())
            .Concat(invocations)
            .SelectMany(GetMemberAccessFirst);

        var locals = body.GetChildren<VariableDeclaratorSyntax>().Select(v => v.Initializer?.Value)
            .OfType<ExpressionSyntax>().SelectMany(GetMemberAccessFirst);

        return [..baseMembers, ..locals];

        static IReadOnlyList<ExpressionSyntax> GetMemberAccessFirst(ExpressionSyntax expression)
        {
            return expression switch
            {
                InvocationExpressionSyntax invocation =>
                [
                    ..invocation.ArgumentList.Arguments.SelectMany(arg => GetMemberAccessFirst(arg.Expression))
                ],
                _ => GetMemberAccess(expression)
            };

            static IReadOnlyList<ExpressionSyntax> GetMemberAccess(ExpressionSyntax exp) => exp switch
            {
                IdentifierNameSyntax name => [name],
                MemberAccessExpressionSyntax member => [member],
                AssignmentExpressionSyntax assignment => GetMemberAccess(assignment.Right),
                BinaryExpressionSyntax binary =>
                [
                    ..GetMemberAccess(binary.Left), ..GetMemberAccess(binary.Right)
                ],
                _ => []
            };
        }
    }

    internal MethodDeclarationSyntax? GetMethodDeclaration() => GetMethodDeclaration(Name.GetName);
    
    private MethodDeclarationSyntax? GetMethodDeclaration(string name)
    {
        var type = Node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        return type?.GetChildren<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == name);
    }
}