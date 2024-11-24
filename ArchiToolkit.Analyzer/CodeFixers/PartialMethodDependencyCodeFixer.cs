using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ArchiToolkit.Analyzer.Resources;

namespace ArchiToolkit.Analyzer.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PartialMethodDependencyCodeFixer : BaseDependencyCodeFixer
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticExtensions.PartialMethodDiagnosticId];

    public override string Tittle =>  CodeFixerStrings.PartialMethodFixerTittle;
    public override string EquivalenceKey =>  CodeFixerStrings.PartialMethodFixerEquivalenceKey;

    protected override Task<Solution> AddPartialAsync(Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax propertyDeclaration)
    {
        var name = new PropDpName(propertyDeclaration.Identifier.Text).GetName;

        var method = MethodDeclaration(
                propertyDeclaration.Type,
                Identifier(name))
            .WithModifiers(
                TokenList(Token(SyntaxKind.PartialKeyword)))
            .WithBody(
                Block(
                    SingletonList<StatementSyntax>(
                        ThrowStatement(
                            ObjectCreationExpression(
                                    IdentifierName("NotImplementedException"))
                                .WithArgumentList(
                                    ArgumentList())))));

        var newRoot = root.InsertNodesAfter(propertyDeclaration, [method]);

        return Task.FromResult(document.WithSyntaxRoot(newRoot).Project.Solution);
    }
}