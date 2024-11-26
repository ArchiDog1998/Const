using ArchiToolkit.Analyzer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

public class FieldPropertyItem(PropertyDeclarationSyntax node, IPropertySymbol symbol)
    : BasePropertyDependencyItem(node, symbol)
{
    private const string ChangePrefix = PropDpName.Prefix + "On";
    
    public override IReadOnlyList<MemberDeclarationSyntax> GetMembers()
        => [.. base.GetMembers(), ..PartialMethods()];

    private IEnumerable<MethodDeclarationSyntax> PartialMethods()
    {
        return [CreatePartialMethod(Name.NameChanging), CreatePartialMethod(Name.NameChanged)];
        
        MethodDeclarationSyntax CreatePartialMethod(string methodName) => MethodDeclaration(
                PredefinedType(
                    Token(SyntaxKind.VoidKeyword)),
                Identifier(ChangePrefix + methodName))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PartialKeyword)))
            .WithParameterList(
                ParameterList(
                    SingletonSeparatedList(
                        Parameter(
                                Identifier("value"))
                            .WithType(
                                IdentifierName(TypeName)))))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken));
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
    
    private IReadOnlyList<StatementSyntax> ChangedInvoke()
    {
        var changed = ExpressionStatement(ConditionalAccessExpression(IdentifierName(Name.NameChanged),
            InvocationExpression(MemberBindingExpression(IdentifierName("Invoke")))));

        var changedMethod = CreateInvocationMethod(Name.NameChanged);
        
        return [changed, changedMethod, InvokeEvent("PropertyChanged")];
    }

    internal static INamedTypeSymbol? GetTypeArgument(IPropertySymbol symbol)
    {
        var attr = symbol.GetAttributes().FirstOrDefault(attr =>
            attr.AttributeClass?.GetFullMetadataName() is FieldDependencyAnalyzer.AttributeName);
        return attr?.NamedArguments.FirstOrDefault(arg => arg.Key == "Comparer").Value.Value as INamedTypeSymbol;
    }

    internal static bool IsValidType(IPropertySymbol symbol, INamedTypeSymbol type)
    {
        if (!type.Constructors.Any(c => c.Parameters.Length == 0 && c.TypeArguments.Length == 0)) return false;
        var findName = $"System.Collections.Generic.IEqualityComparer<{symbol.Type.GetFullMetadataName()}>";
        return type.AllInterfaces.Any(i => i.GetFullMetadataName() == findName);
    }
    
    private IReadOnlyList<StatementSyntax> ChangingInvoke()
    {
        ExpressionSyntax expression = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            GenericName(
                    Identifier("global::System.Collections.Generic.EqualityComparer"))
                .WithTypeArgumentList(
                    TypeArgumentList(
                        SingletonSeparatedList<TypeSyntax>(
                            IdentifierName(TypeName)))),
            IdentifierName("Default"));


        var comparer = GetTypeArgument(Symbol);
        if (comparer is not null && IsValidType(Symbol, comparer))
        {
            expression = ObjectCreationExpression(
                    IdentifierName(comparer.GetFullMetadataName()))
                .WithArgumentList(
                    ArgumentList());
        }
        
        var ifReturn = IfStatement(
            InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        expression, IdentifierName("Equals")))
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

        var changingMethod = CreateInvocationMethod(Name.NameChanging);

        return [ifReturn, changing, changingMethod, InvokeEvent("PropertyChanging")];
    }

    private static ExpressionStatementSyntax CreateInvocationMethod(string methodName) => ExpressionStatement(
        InvocationExpression(
                IdentifierName(ChangePrefix + methodName))
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            IdentifierName(
                                Identifier(
                                    TriviaList(),
                                    SyntaxKind.FieldKeyword,
                                    "field",
                                    "field",
                                    TriviaList())))))));

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
}