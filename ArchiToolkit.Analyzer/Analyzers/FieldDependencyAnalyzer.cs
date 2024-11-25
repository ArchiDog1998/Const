using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ArchiToolkit.Analyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]

public class FieldDependencyAnalyzer: DependencyAnalyzer
{
    internal const string AttributeName = "ArchiToolkit.FieldDpAttribute";

    protected override ImmutableArray<DiagnosticDescriptor> CustomDiagnostics => [];
    protected override string DetectAttributeName => AttributeName;
    protected override void CustomCheck(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node, SemanticModel model)
    {
    }
}