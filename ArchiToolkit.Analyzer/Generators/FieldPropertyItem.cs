using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

internal class FieldPropertyItem(PropertyDeclarationSyntax node, IPropertySymbol symbol)
: BasePropertyDependencyItem(node, symbol)
{
    protected override AccessorDeclarationSyntax? UpdateAccess(AccessorDeclarationSyntax accessor)
    {
        return accessor.Kind() switch
        {
            SyntaxKind.GetAccessorDeclaration => accessor,
            SyntaxKind.SetAccessorDeclaration => accessor.WithSemicolonToken(Token(SyntaxKind.None))
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
                    IdentifierName(Identifier(TriviaList(), SyntaxKind.FieldKeyword, "field", "field", TriviaList())),
                    IdentifierName("value"))),
                ExpressionStatement(ConditionalAccessExpression(IdentifierName(Name.OnNameChanged),
                    InvocationExpression(MemberBindingExpression(IdentifierName("Invoke"))))))),
            _ => null
        };
    }
}