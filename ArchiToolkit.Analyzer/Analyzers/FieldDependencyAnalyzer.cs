using System.Collections.Immutable;
using ArchiToolkit.Analyzer.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ArchiToolkit.Analyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]

public class FieldDependencyAnalyzer: DependencyAnalyzer
{
    internal const string AttributeName = "ArchiToolkit.FieldDpAttribute";

    protected override ImmutableArray<DiagnosticDescriptor> CustomDiagnostics => DiagnosticExtensions.FieldDpDescriptors;
    protected override string DetectAttributeName => AttributeName;
    protected override void CustomCheck(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node, SemanticModel model)
    {
        if (model.GetDeclaredSymbol(node) is not { } symbol) return;
        var typeArgument = FieldPropertyItem.GetTypeArgument(symbol);
        if (typeArgument is null) return;
        if (FieldPropertyItem.IsValidType(symbol, typeArgument)) return;

        var attribute = node.AttributeLists.SelectMany(a => a.Attributes).FirstOrDefault(attr => attr.GetTypeSymbol(model).GetFullMetadataName() == AttributeName);

        var argument = attribute?.ArgumentList?.Arguments.FirstOrDefault(arg => arg.NameEquals?.Name.ToString() == "Comparer");
        if (argument is null) return;
        
        context.ReportAttributeType(argument); 
    }
}