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
    
    private static void Execute(SourceProductionContext ctx, ImmutableArray<(PropertyDeclarationSyntax Node, SemanticModel SemanticModel)> list)
    {
        List<BasePropertyDependencyItem> props = [];
        foreach (var (node, model) in list)
        {
            if (!node.Modifiers.Any(SyntaxKind.PartialKeyword)) continue;
            if (model.GetDeclaredSymbol(node) is not { } symbol) continue;
            
            PropertyDependencyAnalyzer.CheckAccessors(node, out var hasGet, out var hasSet);
            if (!hasGet) continue;

            props.Add(hasSet ? new FieldPropertyItem(node, symbol) : new MethodPropertyItem(node, symbol));
        }

        foreach (var prop in props)
        {
            SaveMembers(ctx, prop);
        }
    }

    private static void SaveMembers(SourceProductionContext ctx, BasePropertyDependencyItem basePropertyDependencyItem)
    {
        var members = basePropertyDependencyItem.GetMembers();
        if (members.Count == 0) return;
        
        var node = basePropertyDependencyItem.Node;
        var parents = node.Parent?.AncestorsAndSelf().ToImmutableArray();
        var type = parents?.OfType<TypeDeclarationSyntax>().FirstOrDefault();
        var nameSpace = parents?.OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        
        if (type is null || nameSpace is null) return;
        
        var code = NamespaceDeclaration(nameSpace.Name.ToFullString())
            .WithMembers(
                SingletonList<MemberDeclarationSyntax>(ClassDeclaration(type.Identifier.Text)
                    .WithModifiers(type.Modifiers)
                    .WithMembers(List(members))))
            .NodeToString();
            
        var symbol = basePropertyDependencyItem.Symbol;
        ctx.AddSource($"{symbol.GetFullMetadataName()}.{node.Identifier.Text}.g.cs", code);
    }
}