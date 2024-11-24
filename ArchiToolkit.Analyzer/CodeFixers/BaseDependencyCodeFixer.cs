using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.CodeFixers;

public abstract class BaseDependencyCodeFixer : CodeFixProvider
{
    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public abstract string Tittle { get; }
    public abstract string EquivalenceKey { get; }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<PropertyDeclarationSyntax>().First();

        if (declaration is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: string.Format(Tittle, declaration.Identifier.Text),
                createChangedSolution: _ => AddPartialAsync(context.Document, root!, declaration),
                equivalenceKey: EquivalenceKey),
            diagnostic);
    }

    protected abstract Task<Solution> AddPartialAsync(Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax propertyDeclaration);
}