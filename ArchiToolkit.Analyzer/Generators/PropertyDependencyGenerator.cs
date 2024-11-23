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
            static (node, _) => node is BasePropertyDeclarationSyntax,
            static (n, _) => ((BasePropertyDeclarationSyntax)n.TargetNode, n.SemanticModel));

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private static void Execute(SourceProductionContext ctx, ImmutableArray<(BasePropertyDeclarationSyntax Node, SemanticModel SemanticModel)> list)
    {
        foreach (var (node, model) in list)
        {
            if (!node.Modifiers.Any(SyntaxKind.PartialKeyword)) continue;
        }
    }
}