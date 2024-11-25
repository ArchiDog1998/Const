using ArchiToolkit.Analyzer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

[Generator(LanguageNames.CSharp)]

public class FieldDependencyGenerator: DependencyGenerator<FieldPropertyItem>
{
    protected override string AttributeName => FieldDependencyAnalyzer.AttributeName;

    protected override FieldPropertyItem? CreateInstance(PropertyDeclarationSyntax node, IPropertySymbol symbol, SemanticModel model,
        bool hasGet, bool hasSet)
    {
        if (!hasGet || !hasSet) return null;
        return new FieldPropertyItem(node, symbol);
    }
    
    protected override void SaveMembers(SourceProductionContext ctx, IGrouping<TypeDeclarationSyntax, FieldPropertyItem> item)
    {
        var type = item.Key;
        if (type is null) return;
        if (!item.Any()) return;

        var changing = EventFieldDeclaration(
                VariableDeclaration(
                        NullableType(
                            IdentifierName("global::System.ComponentModel.PropertyChangingEventHandler")))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier("PropertyChanging")))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)));
        
        var changed = EventFieldDeclaration(
                VariableDeclaration(
                        NullableType(
                            IdentifierName("global::System.ComponentModel.PropertyChangedEventHandler")))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier("PropertyChanged")))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)));

        SaveMembers(ctx, [changing, changed], type, $"{item.First().Symbol.GetFullMetadataName()}.Type.Notify", BaseList(
            SeparatedList<BaseTypeSyntax>(
                new SyntaxNodeOrToken[]
                {
                    SimpleBaseType(
                        IdentifierName("global::System.ComponentModel.INotifyPropertyChanging")),
                    Token(SyntaxKind.CommaToken),
                    SimpleBaseType(
                        IdentifierName("global::System.ComponentModel.INotifyPropertyChanged"))
                })));
    }
}