using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

public class MethodPropertyItem(PropertyDeclarationSyntax node, IPropertySymbol symbol)
    : BasePropertyDependencyItem(node, symbol)
{
 
    public override IReadOnlyList<MemberDeclarationSyntax> GetMembers() =>
        [..base.GetMembers(), GetLazyField(), GetMethod(), ClearMethod()];

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
            .WithBody(
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
                            IdentifierName(Name.OnNameChanging),
                            InvocationExpression(
                                MemberBindingExpression(
                                    IdentifierName("Invoke"))))),
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
                            IdentifierName(Name.OnNameChanged),
                            InvocationExpression(
                                MemberBindingExpression(
                                    IdentifierName("Invoke")))))));
    }
}