using System.Collections.Immutable;
using ArchiToolkit.Analyzer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

[Generator(LanguageNames.CSharp)]
public class PropertyDependencyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            PropertyDependencyAnalyzer.AttributeName,
            static (node, _) => node is PropertyDeclarationSyntax,
            static (n, _) => ((PropertyDeclarationSyntax)n.TargetNode, n.SemanticModel));

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private static void Execute(SourceProductionContext ctx,
        ImmutableArray<(PropertyDeclarationSyntax Node, SemanticModel SemanticModel)> list)
    {
        List<BasePropertyDependencyItem> props = [];
        foreach (var (node, model) in list)
        {
            if (!node.Modifiers.Any(SyntaxKind.PartialKeyword)) continue;
            if (node.Modifiers.Any(SyntaxKind.StaticKeyword)) continue;
            if (model.GetDeclaredSymbol(node) is not { } symbol) continue;

            PropertyDependencyAnalyzer.CheckAccessors(node, out var hasGet, out var hasSet);
            if (!hasGet) continue;

            props.Add(hasSet ? new FieldPropertyItem(node, symbol) : new MethodPropertyItem(node, symbol, model));
        }

        foreach (var prop in props)
        {
            SaveMembers(ctx, prop);
        }
        
        foreach (var grp in props.OfType<MethodPropertyItem>()
                     .GroupBy(i => i.Node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() ))
        {
            SaveMembers(ctx, grp);
        }
        
        foreach (var grp in props.OfType<FieldPropertyItem>()
                     .GroupBy(i => i.Node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() ))
        {
            SaveMembers(ctx, grp);
        }
    }

    private static void SaveMembers(SourceProductionContext ctx, IGrouping<TypeDeclarationSyntax, FieldPropertyItem> item)
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
    
    private static void SaveMembers(SourceProductionContext ctx, IGrouping<TypeDeclarationSyntax, MethodPropertyItem> item)
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


    private static void SaveMembers(SourceProductionContext ctx, BasePropertyDependencyItem basePropertyDependencyItem)
    {
        var members = basePropertyDependencyItem.GetMembers();
        if (members.Count == 0) return;

        var node = basePropertyDependencyItem.Node;
        var type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (type is null) return;

        var symbol = basePropertyDependencyItem.Symbol;

        SaveMembers(ctx, members, type, $"{symbol.GetFullMetadataName()}.{node.Identifier.Text}");
    }

    private static void SaveMembers(SourceProductionContext ctx, IReadOnlyList<MemberDeclarationSyntax> members,
        TypeDeclarationSyntax type, string name, BaseListSyntax? baseListSyntax = null)
    {
        var nameSpace = type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();

        if (nameSpace is null) return;
        
        if (baseListSyntax is not null)
        {
            type = type.WithBaseList(baseListSyntax);
        }

        var code = NamespaceDeclaration(nameSpace.Name.ToFullString())
            .WithMembers(
                SingletonList<MemberDeclarationSyntax>(type
                    .WithMembers(List(members))))
            .NodeToString();

        ctx.AddSource($"{name}.g.cs", code);
    }
}