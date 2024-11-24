using System.Collections.Immutable;
using ArchiToolkit.Analyzer.Generators;
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
        var model = context.SemanticModel;

        if (model.GetDeclaredSymbol(node) is not { } symbol) return;
        if (!symbol.GetAttributes().Any(a => a.AttributeClass?.GetFullMetadataName() is AttributeName)) return;
        
        PartialCheck(context, node);
        AccessorsCheck(context, node);
        PartialMethodCheck(context, node, model);
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

    private static void PartialMethodCheck(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax node, SemanticModel model)
    {
        CheckAccessors(node, out var hasGetter, out var hasSetter);
        if (!hasGetter || hasSetter) return;

        if (model.GetDeclaredSymbol(node) is not { } symbol) return;

        var property = new MethodPropertyItem(node, symbol, model);
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

    
    internal static void CheckAccessors(PropertyDeclarationSyntax node, out bool hasGetter, out bool hasSetter)
    {
        hasGetter = hasSetter = false;
            
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
            }
        }
    }
}