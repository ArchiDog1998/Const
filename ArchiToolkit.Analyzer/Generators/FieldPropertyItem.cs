using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

public class FieldPropertyItem(PropertyDeclarationSyntax node, IPropertySymbol symbol)
    : BasePropertyDependencyItem(node, symbol)
{
    private IReadOnlyList<StatementSyntax> ChangedInvoke()
    {
        var changed = ExpressionStatement(ConditionalAccessExpression(IdentifierName(Name.NameChanged),
            InvocationExpression(MemberBindingExpression(IdentifierName("Invoke")))));

        return [changed, InvokeEvent("PropertyChanged")];
    }

    private IReadOnlyList<StatementSyntax> ChangingInvoke()
    {
        var ifReturn = IfStatement(
            InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(
                        Identifier(TriviaList(), SyntaxKind.FieldKeyword, "field", "field", TriviaList())),
                    IdentifierName("Equals")))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName("value"))))),
            ReturnStatement());
        var changing = ExpressionStatement(ConditionalAccessExpression(IdentifierName(Name.NameChanging),
            InvocationExpression(MemberBindingExpression(IdentifierName("Invoke")))));

        return [ifReturn, changing, InvokeEvent("PropertyChanging")];
    }

    private StatementSyntax InvokeEvent(string eventName)
    {
        return ExpressionStatement(
            ConditionalAccessExpression(
                IdentifierName(eventName),
                InvocationExpression(
                        MemberBindingExpression(
                            IdentifierName("Invoke")))
                    .WithArgumentList(
                        ArgumentList(
                            SeparatedList<ArgumentSyntax>(
                                new SyntaxNodeOrToken[]
                                {
                                    Argument(
                                        ThisExpression()),
                                    Token(SyntaxKind.CommaToken),
                                    Argument(
                                        ImplicitObjectCreationExpression()
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            InvocationExpression(
                                                                    IdentifierName(
                                                                        Identifier(
                                                                            TriviaList(),
                                                                            SyntaxKind.NameOfKeyword,
                                                                            "nameof",
                                                                            "nameof",
                                                                            TriviaList())))
                                                                .WithArgumentList(
                                                                    ArgumentList(
                                                                        SingletonSeparatedList(
                                                                            Argument(
                                                                                IdentifierName(Name.Name))))))))))
                                })))));
    }

    private StatementSyntax Assign()
    {
        return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
            IdentifierName(
                Identifier(TriviaList(), SyntaxKind.FieldKeyword, "field", "field", TriviaList())),
            IdentifierName("value")));
    }
    
    protected override AccessorDeclarationSyntax? UpdateAccess(AccessorDeclarationSyntax accessor)
    {
        return accessor.Kind() switch
        {
            SyntaxKind.GetAccessorDeclaration => accessor,
            SyntaxKind.SetAccessorDeclaration when Symbol.Type.IsReferenceType => accessor
                .WithSemicolonToken(Token(SyntaxKind.None))
                .WithBody(Block(
                            (StatementSyntax[])[IfStatement(
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
                                Block(ChangingInvoke())), Assign(), ..ChangedInvoke()])),

            SyntaxKind.SetAccessorDeclaration => accessor
                .WithSemicolonToken(Token(SyntaxKind.None))
                .WithBody(Block((StatementSyntax[])[..ChangingInvoke(), Assign(), ..ChangedInvoke()])),
            _ => null
        };
    }
}