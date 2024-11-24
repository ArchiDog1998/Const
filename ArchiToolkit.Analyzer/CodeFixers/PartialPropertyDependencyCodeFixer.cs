using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PartialPropertyDependencyCodeFixer : BaseDependencyCodeFixer
{
    public override ImmutableArray<string> FixableDiagnosticIds  => [DiagnosticExtensions.PartialPropertyDiagnosticId];
    public override string Tittle => "Add partial keyword for the property '{0}'";
    public override string EquivalenceKey => "Add partial keyword";

    protected override Task<Solution> AddPartialAsync(Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax propertyDeclaration)
    {
        var newNode = propertyDeclaration.WithModifiers(propertyDeclaration.Modifiers.Add(Token(SyntaxKind.PartialKeyword)));
        
        var newRoot = root.ReplaceNode(propertyDeclaration, newNode);
        
        return Task.FromResult(document.WithSyntaxRoot(newRoot).Project.Solution);
    }
}