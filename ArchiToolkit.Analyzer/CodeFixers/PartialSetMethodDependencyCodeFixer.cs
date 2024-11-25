using System.Collections.Immutable;
using ArchiToolkit.Analyzer.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PartialSetMethodDependencyCodeFixer : BaseDependencyCodeFixer
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticExtensions.PartialSetMethodDiagnosticId];
    protected override string Tittle => CodeFixerStrings.PartialSetMethodFixerTittle;
    protected override string EquivalenceKey => CodeFixerStrings.PartialSetMethodFixerEquivalenceKey;

    protected override Task<Solution> AddPartialAsync(Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax propertyDeclaration)
    {
        var name = new PropDpName(propertyDeclaration.Identifier.Text).SetName;

        var method = MethodDeclaration(
                PredefinedType(
                    Token(SyntaxKind.VoidKeyword)),
                Identifier(name))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PartialKeyword)))
            .WithParameterList(
                ParameterList(
                    SingletonSeparatedList(
                        Parameter(
                                Identifier("value"))
                            .WithType(propertyDeclaration.Type))))
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