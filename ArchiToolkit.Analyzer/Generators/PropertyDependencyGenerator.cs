using System.Collections.Immutable;
using ArchiToolkit.Analyzer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

[Generator(LanguageNames.CSharp)]
public class PropertyDependencyGenerator : DependencyGenerator<MethodPropertyItem>
{
    protected override string AttributeName => PropertyDependencyAnalyzer.AttributeName;

    protected override MethodPropertyItem? CreateInstance(PropertyDeclarationSyntax node, 
        IPropertySymbol symbol, SemanticModel model, bool hasGet, bool hasSet)
    {
        if (!hasGet) return null;
        return new MethodPropertyItem(node, symbol, model, hasSet);
    }
    
    protected override void SaveMembers(SourceProductionContext ctx, IGrouping<TypeDeclarationSyntax, MethodPropertyItem> item)
    {
        const string initName = "Initialize";
        
        var type = item.Key;
        if (type is null) return;
        
        if(!item.Any()) return;
        
        var invokes = item.Select(i => i.Name.InitName).Append(initName).Select(n =>
            ExpressionStatement(InvocationExpression(IdentifierName(n))));
        var ctr = Ctor(type, invokes);
        var symbol = item.First().Symbol;
        SaveMembers(ctx, [InitializeMethod(), ctr], type, $"{symbol.GetFullMetadataName()}.Type.Ctor");
        return;

        static MethodDeclarationSyntax InitializeMethod()
        {
            return MethodDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.VoidKeyword)),
                    Identifier(initName))
                .WithModifiers(
                    TokenList(Token(SyntaxKind.PartialKeyword)))
                .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
                .WithSemicolonToken(
                    Token(SyntaxKind.SemicolonToken));
        }

        static ConstructorDeclarationSyntax Ctor(TypeDeclarationSyntax type, IEnumerable<ExpressionStatementSyntax> statements)
        {
            return ConstructorDeclaration(
                    Identifier(type.Identifier.Text))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
                .WithBody(
                    Block(statements));
        }
    }
}