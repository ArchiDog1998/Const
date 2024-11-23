using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PropertyDependencyCodeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds  => [DiagnosticExtensions.PartialPropertyDiagnosticId];
    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<PropertyDeclarationSyntax>().First();

        if (declaration is null) return;

        context.RegisterCodeFix(
            CodeAction.Create( //TODO: i18n for this!
                title: $"Add partial keyword for the property '{declaration.Identifier.Text}'",
                createChangedSolution: _ => MakeUppercaseAsync(context.Document, root!, declaration),
                equivalenceKey: "Add partial keyword"),
            diagnostic);
    }

    private static Task<Solution> MakeUppercaseAsync(Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax propertyDeclaration)
    {
        var newNode = propertyDeclaration.WithModifiers(propertyDeclaration.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword)));
        
        var newRoot = root.ReplaceNode(propertyDeclaration, newNode);
        
        return Task.FromResult(document.WithSyntaxRoot(newRoot).Project.Solution);
    }
}