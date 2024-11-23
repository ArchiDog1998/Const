using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

public class MethodPropertyItem(PropertyDeclarationSyntax node, IPropertySymbol symbol)
    : BasePropertyDependencyItem(node, symbol)
{
    public string ClearName => "Clear" + Name;
    public string GetName => "Get" + Name;
    private string LazyName => "_" + Name;

    public override IReadOnlyList<MemberDeclarationSyntax> GetMembers() =>
        [..base.GetMembers(), GetLazyField(), GetMethod(), ClearMethod()];

    protected override AccessorDeclarationSyntax UpdateAccess(AccessorDeclarationSyntax accessor)
    {
        if (accessor.Kind() is not SyntaxKind.GetAccessorDeclaration) return accessor;
        return accessor.WithExpressionBody(
            ArrowExpressionClause(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(LazyName),
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
                                Identifier(LazyName)))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword)));
    }

    private MethodDeclarationSyntax GetMethod()
    {
        return MethodDeclaration(
                IdentifierName(TypeName),
                Identifier(GetName))
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
                Identifier(ClearName))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword)))
            .WithBody(
                Block(
                    IfStatement(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(LazyName),
                            IdentifierName("IsValueCreated")),
                        Block(
                            SingletonList<StatementSyntax>(
                                ExpressionStatement(
                                    ConditionalAccessExpression(
                                        IdentifierName(OnNameChanging),
                                        InvocationExpression(
                                            MemberBindingExpression(
                                                IdentifierName("Invoke")))))))),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(LazyName),
                            ImplicitObjectCreationExpression()
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                IdentifierName(GetName))))))),
                    ExpressionStatement(
                        ConditionalAccessExpression(
                            IdentifierName(OnNameChanged),
                            InvocationExpression(
                                MemberBindingExpression(
                                    IdentifierName("Invoke")))))));
    }
}