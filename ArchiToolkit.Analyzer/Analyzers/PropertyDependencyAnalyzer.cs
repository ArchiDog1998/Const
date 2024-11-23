using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ArchiToolkit.Analyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PropertyDependencyAnalyzer : DiagnosticAnalyzer
{
    internal const string AttributeName = "ArchiToolkit.PropDpAttribute";

    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => DiagnosticExtensions.PropDpDescriptors;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                               GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not PropertyDeclarationSyntax node) return;
        if (node.Modifiers.Any(SyntaxKind.PartialKeyword)) return;
        var model = context.SemanticModel;

        if (model.GetDeclaredSymbol(node) is not { } symbol) return;
        if (!symbol.GetAttributes().Any(a => a.AttributeClass?.GetFullMetadataName() is AttributeName)) return;

        context.ReportPartial(node.Identifier);
    }
}