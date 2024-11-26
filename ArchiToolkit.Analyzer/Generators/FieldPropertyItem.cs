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
            InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GenericName(
                                    Identifier("global::System.Collections.Generic.EqualityComparer"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(TypeName)))),
                            IdentifierName("Default")),
                        IdentifierName("Equals")))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList<ArgumentSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                Argument(
                                    IdentifierName(
                                        Identifier(
                                            TriviaList(),
                                            SyntaxKind.FieldKeyword,
                                            "field",
                                            "field",
                                            TriviaList()))),
                                Token(SyntaxKind.CommaToken),
                                Argument(
                                    IdentifierName("value"))
                            }))),
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

    private static StatementSyntax Assign()
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
            SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration => accessor
                .WithSemicolonToken(Token(SyntaxKind.None))
                .WithBody(Block((StatementSyntax[])[..ChangingInvoke(), Assign(), ..ChangedInvoke()])),
            _ => null
        };
    }
}