using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

internal class FieldPropertyItem(PropertyDeclarationSyntax node, IPropertySymbol symbol)
    : BasePropertyDependencyItem(node, symbol)
{
    protected override AccessorDeclarationSyntax? UpdateAccess(AccessorDeclarationSyntax accessor)
    {
        accessor = AccessorDeclaration(accessor.Kind())
            .WithModifiers(TokenList(accessor.Modifiers.Select(m => Token(m.Kind()))));


        return accessor.Kind() switch
        {
            SyntaxKind.GetAccessorDeclaration => accessor.WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
            SyntaxKind.SetAccessorDeclaration when Symbol.Type.IsReferenceType => accessor
                .WithBody(Block(
                    IfStatement(
                        IsPatternExpression(
                            IdentifierName(
                                Identifier(
                                    TriviaList(),
                                    SyntaxKind.FieldKeyword,
                                    "field",
                                    "field",
                                    TriviaList())),
                            UnaryPattern(
                                ConstantPattern(
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression)))),
                        Block(
                            IfStatement(
                                InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(
                                                Identifier(
                                                    TriviaList(),
                                                    SyntaxKind.FieldKeyword,
                                                    "field",
                                                    "field",
                                                    TriviaList())),
                                            IdentifierName("Equals")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList<ArgumentSyntax>(
                                                Argument(
                                                    IdentifierName("value"))))),
                                ReturnStatement()),
                            ExpressionStatement(
                                ConditionalAccessExpression(
                                    IdentifierName(Name.OnNameChanging),
                                    InvocationExpression(
                                        MemberBindingExpression(
                                            IdentifierName("Invoke"))))))),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(
                                Identifier(
                                    TriviaList(),
                                    SyntaxKind.FieldKeyword,
                                    "field",
                                    "field",
                                    TriviaList())),
                            IdentifierName("value"))),
                    ExpressionStatement(
                        ConditionalAccessExpression(
                            IdentifierName(Name.OnNameChanged),
                            InvocationExpression(
                                MemberBindingExpression(
                                    IdentifierName("Invoke"))))))),

            SyntaxKind.SetAccessorDeclaration => accessor
                .WithBody(Block(
                    IfStatement(
                        InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(
                                    Identifier(TriviaList(), SyntaxKind.FieldKeyword, "field", "field", TriviaList())),
                                IdentifierName("Equals")))
                            .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName("value"))))),
                        ReturnStatement()),
                    ExpressionStatement(ConditionalAccessExpression(IdentifierName(Name.OnNameChanging),
                        InvocationExpression(MemberBindingExpression(IdentifierName("Invoke"))))),
                    ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(
                            Identifier(TriviaList(), SyntaxKind.FieldKeyword, "field", "field", TriviaList())),
                        IdentifierName("value"))),
                    ExpressionStatement(ConditionalAccessExpression(IdentifierName(Name.OnNameChanged),
                        InvocationExpression(MemberBindingExpression(IdentifierName("Invoke"))))))),
            _ => null
        };
    }
}