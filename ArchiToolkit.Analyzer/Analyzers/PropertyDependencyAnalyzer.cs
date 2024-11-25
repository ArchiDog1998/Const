using System.Collections.Immutable;
using ArchiToolkit.Analyzer.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ArchiToolkit.Analyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PropertyDependencyAnalyzer : DependencyAnalyzer
{
    internal const string AttributeName = "ArchiToolkit.PropDpAttribute";

    protected override string DetectAttributeName => AttributeName;

    protected override ImmutableArray<DiagnosticDescriptor> CustomDiagnostics => DiagnosticExtensions.PropDpDescriptors;

    protected override void CustomCheck(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node, SemanticModel model)
    {
        PartialMethodCheck(context, node, model);
    }
    
    private static void PartialMethodCheck(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node, SemanticModel model)
    {
        CheckAccessors(node, out var hasGetter, out var hasSetter, out _);
        if (!hasGetter) return;
        
        if (model.GetDeclaredSymbol(node) is not { } symbol) return;
        
        var property = new MethodPropertyItem(node, symbol, model, hasSetter);
        var method = property.GetMethodDeclaration();
        
        PartialMethodExistenceCheck();
        PartialMethodCallSelfCheck();
        
        return;
        
        void PartialMethodExistenceCheck()
        {
            if (method is not null) return;
            context.ReportPartialMethod(node.Identifier);
        }
        
        void PartialMethodCallSelfCheck()
        {
            var accessors = property.GetAccessItems()
                .Where(i => i.HasSymbol(symbol)).ToImmutableArray();
        
            if (!accessors.Any()) return;
        
            var accessor = accessors.First();
        
            context.ReportPartialMethodCallSelf(accessor.Expression);
        }
    }
    
    internal static void CheckAccessors(PropertyDeclarationSyntax node,
        out bool hasGetter, out bool hasSetter, out bool hasOthers)
    {
        hasGetter = hasSetter = hasOthers = false;

        var accessors = node.AccessorList?.Accessors;
        if (accessors == null) return;

        foreach (var accessor in accessors)
        {
            switch (accessor.Kind())
            {
                case SyntaxKind.GetAccessorDeclaration:
                    hasGetter = true;
                    break;
                case SyntaxKind.SetAccessorDeclaration:
                    hasSetter = true;
                    break;
                default:
                    hasOthers = true;
                    break;
            }
        }
    }
}