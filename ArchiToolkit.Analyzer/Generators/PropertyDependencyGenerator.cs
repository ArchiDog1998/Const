using System.Collections.Immutable;
using ArchiToolkit.Analyzer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

[Generator(LanguageNames.CSharp)]
public class PropertyDependencyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            PropertyDependencyAnalyzer.AttributeName,
            static (node, _) => node is PropertyDeclarationSyntax,
            static (n, _) => ((PropertyDeclarationSyntax)n.TargetNode, n.SemanticModel));

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private static void Execute(SourceProductionContext ctx,
        ImmutableArray<(PropertyDeclarationSyntax Node, SemanticModel SemanticModel)> list)
    {
        List<BasePropertyDependencyItem> props = [];
        foreach (var (node, model) in list)
        {
            if (!node.Modifiers.Any(SyntaxKind.PartialKeyword)) continue;
            if (model.GetDeclaredSymbol(node) is not { } symbol) continue;

            PropertyDependencyAnalyzer.CheckAccessors(node, out var hasGet, out var hasSet);
            if (!hasGet) continue;

            props.Add(hasSet ? new FieldPropertyItem(node, symbol) : new MethodPropertyItem(node, symbol, model));
        }

        foreach (var prop in props)
        {
            SaveMembers(ctx, prop);
        }
        
        foreach (var grp in props.OfType<MethodPropertyItem>()
                     .GroupBy(p => p.Symbol.Type, SymbolEqualityComparer.Default))
        {
            SaveMembers(ctx, grp.ToArray());
        }
    }

    private static void SaveMembers(SourceProductionContext ctx, MethodPropertyItem[] methodItems)
    {
        const string initName = "Initialize";
        
        if (methodItems.Length == 0) return;
        var node = methodItems[0].Node;
        var type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (type is null) return;

        var invokes = methodItems.Select(i => i.Name.InitName).Append(initName).Select(n =>
            ExpressionStatement(InvocationExpression(IdentifierName(n))));
        var ctr = Ctor(type, invokes);
        var symbol = methodItems[0].Symbol;
        SaveMembers(ctx, [InitializeMethod(), ctr], type, $"{symbol.GetFullMetadataName()}.Ctor");
        return;

        static MethodDeclarationSyntax InitializeMethod()
        {
            return MethodDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.VoidKeyword)),
                    Identifier(initName))
                .WithModifiers(
                    TokenList(Token(SyntaxKind.PartialKeyword)))
                .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
                .WithSemicolonToken(
                    Token(SyntaxKind.SemicolonToken));
        }

        static ConstructorDeclarationSyntax Ctor(TypeDeclarationSyntax type, IEnumerable<ExpressionStatementSyntax> statements)
        {
            return ConstructorDeclaration(
                    Identifier(type.Identifier.Text))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
                .WithBody(
                    Block(statements));
        }
    }


    private static void SaveMembers(SourceProductionContext ctx, BasePropertyDependencyItem basePropertyDependencyItem)
    {
        var members = basePropertyDependencyItem.GetMembers();
        if (members.Count == 0) return;

        var node = basePropertyDependencyItem.Node;
        var type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (type is null) return;

        var symbol = basePropertyDependencyItem.Symbol;

        SaveMembers(ctx, members, type, $"{symbol.GetFullMetadataName()}.{node.Identifier.Text}");
    }

    private static void SaveMembers(SourceProductionContext ctx, IReadOnlyList<MemberDeclarationSyntax> members,
        TypeDeclarationSyntax type, string name)
    {
        var nameSpace = type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();

        if (nameSpace is null) return;

        var code = NamespaceDeclaration(nameSpace.Name.ToFullString())
            .WithMembers(
                SingletonList<MemberDeclarationSyntax>(ClassDeclaration(type.Identifier.Text)
                    .WithModifiers(type.Modifiers)
                    .WithMembers(List(members))))
            .NodeToString();

        ctx.AddSource($"{name}.g.cs", code);
    }
}