using System.Collections.Immutable;
using ArchiToolkit.Analyzer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

file static class DependencyGeneratorData
{
    internal static readonly HashSet<string> AddedNames = [];
}

public abstract class DependencyGenerator<T> : IIncrementalGenerator
    where T : BasePropertyDependencyItem
{
    protected abstract string AttributeName { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            static (node, _) => node is PropertyDeclarationSyntax,
            static (n, _) => ((PropertyDeclarationSyntax)n.TargetNode, n.SemanticModel));

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }
    
    private void Execute(SourceProductionContext ctx,
        ImmutableArray<(PropertyDeclarationSyntax Node, SemanticModel SemanticModel)> list)
    {
        List<T> props = [];
        foreach (var (node, model) in list)
        {
            if (!node.Modifiers.Any(SyntaxKind.PartialKeyword)) continue;
            if (node.Modifiers.Any(SyntaxKind.StaticKeyword)) continue;
            if (model.GetDeclaredSymbol(node) is not { } symbol) continue;

            PropertyDependencyAnalyzer.CheckAccessors(node, out var hasGet, out var hasSet, out var hasOthers);
            if (hasOthers) continue;
            
            var item = CreateInstance(node, symbol, model, hasGet, hasSet);
            if (item is null) continue;

            props.Add(item);
        }

        DependencyGeneratorData.AddedNames.Clear();
        foreach (var prop in props)
        {
            SaveMembers(ctx, prop);
        }
        
        foreach (var grp in props
                     .GroupBy(i => i.Node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() ))
        {
            SaveMembers(ctx, grp);
        }
    }

    protected abstract void SaveMembers(SourceProductionContext ctx, IGrouping<TypeDeclarationSyntax, T> item);
    
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
    
    protected abstract T? CreateInstance(
        PropertyDeclarationSyntax node,
        IPropertySymbol symbol,
        SemanticModel model,
        bool hasGet, bool hasSet);
    
    protected static void SaveMembers(SourceProductionContext ctx, IReadOnlyList<MemberDeclarationSyntax> members,
        TypeDeclarationSyntax type, string name, BaseListSyntax? baseListSyntax = null)
    {
        if (!DependencyGeneratorData.AddedNames.Add(name)) return;

        var nameSpace = type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();

        if (nameSpace is null) return;
        
        type = type.WithBaseList(baseListSyntax);

        var code = NamespaceDeclaration(nameSpace.Name.ToFullString())
            .WithMembers(
                SingletonList<MemberDeclarationSyntax>(type
                    .WithMembers(List(members))))
            .NodeToString();

        ctx.AddSource($"{name}.g.cs", code);
    }
}