using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ArchiToolkit.Analyzer.Analyzers;

public abstract class DependencyAnalyzer: DiagnosticAnalyzer
{
    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        ..DiagnosticExtensions.BaseDpDescriptors,
        ..CustomDiagnostics,
    ];
    
    protected abstract ImmutableArray<DiagnosticDescriptor> CustomDiagnostics { get; } 

    protected abstract string DetectAttributeName { get; }
    public sealed override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                               GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PropertyDeclaration);
    }
    
    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not PropertyDeclarationSyntax node) return;
        var model = context.SemanticModel;

        if (model.GetDeclaredSymbol(node) is not { } symbol) return;
        if (!symbol.GetAttributes().Any(a => a.AttributeClass?.GetFullMetadataName() == DetectAttributeName)) return;

        PartialCheck(context, node);
        PartialStaticCheck(context, node);
        AccessorsCheck(context, node);

        CustomCheck(context, node, model);
    }
    
    private static void PartialCheck(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node)
    {
        if (node.Modifiers.Any(SyntaxKind.PartialKeyword)) return;
        context.ReportPartial(node.Identifier);
    }
    
    private static void AccessorsCheck(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node)
    {
        var accessors = node.AccessorList?.Accessors;

        if (accessors == null) return;

        foreach (var accessor in accessors)
        {
            AccessorCheck(accessor);
        }

        return;

        void AccessorCheck(AccessorDeclarationSyntax accessor)
        {
            if (accessor.Body is not null)
            {
                context.ReportBody(accessor.Body);
            }

            if (accessor.Kind() is not SyntaxKind.GetAccessorDeclaration and not SyntaxKind.SetAccessorDeclaration)
            {
                context.ReportAccessorType(accessor);
            }
        }
    }
    
    private static void PartialStaticCheck(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node)
    {
        if (!node.Modifiers.Any(SyntaxKind.StaticKeyword)) return;
        context.ReportPartialStatic(node.Identifier);
    }

    protected abstract void CustomCheck(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node,
        SemanticModel model);
}