using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ArchiToolkit.Analyzer.Resources;

namespace ArchiToolkit.Analyzer.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PartialPropertyDependencyCodeFixer : BaseDependencyCodeFixer
{
    public override ImmutableArray<string> FixableDiagnosticIds  => [DiagnosticExtensions.PartialPropertyDiagnosticId];
    public override string Tittle => CodeFixerStrings.PartialPropertyFixerTittle;
    public override string EquivalenceKey =>  CodeFixerStrings.PartialPropertyFixerEquivalenceKey;

    protected override Task<Solution> AddPartialAsync(Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax propertyDeclaration)
    {
        var newNode = propertyDeclaration.WithModifiers(propertyDeclaration.Modifiers.Add(Token(SyntaxKind.PartialKeyword)));
        
        var newRoot = root.ReplaceNode(propertyDeclaration, newNode);
        
        return Task.FromResult(document.WithSyntaxRoot(newRoot).Project.Solution);
    }
}