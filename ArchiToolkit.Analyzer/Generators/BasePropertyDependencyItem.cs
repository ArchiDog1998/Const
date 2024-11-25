using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

public abstract class BasePropertyDependencyItem(PropertyDeclarationSyntax node, IPropertySymbol symbol)
{
    public PropertyDeclarationSyntax Node => node;
    public IPropertySymbol Symbol => symbol;
    public virtual IReadOnlyList<MemberDeclarationSyntax> GetMembers() => [CreateEvents(), CreateProperty()];
    public PropDpName Name => new(Node.Identifier.Text);
    public string TypeName => "global::" + symbol.Type.GetFullMetadataName();
    
    private EventFieldDeclarationSyntax CreateEvents()
    {
        return EventFieldDeclaration(
                VariableDeclaration(
                        NullableType(
                            IdentifierName("global::System.Action")))
                    .WithVariables(
                        SeparatedList<VariableDeclaratorSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                VariableDeclarator(
                                    Identifier(Name.NameChanged)),
                                Token(SyntaxKind.CommaToken),
                                VariableDeclarator(
                                    Identifier(Name.NameChanging))
                            })))
            .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)));
    }


    private PropertyDeclarationSyntax CreateProperty()
    {
        var accessors = node.AccessorList?.Accessors;

        if (accessors is null) return node;

        List<AccessorDeclarationSyntax> resultAccessors = [];
        foreach (var accessor in accessors)
        {
            var newAccessor = UpdateAccess(accessor)?
                    .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))));
            if (newAccessor is null) continue;
            resultAccessors.Add(newAccessor);
        }
        
        return PropertyDeclaration(IdentifierName(TypeName), Identifier(node.Identifier.Text))
            .WithModifiers(TokenList(node.Modifiers.Select(m => Token(m.Kind()))))
            .WithAccessorList(AccessorList(List(resultAccessors)));
    }
    
    protected abstract AccessorDeclarationSyntax? UpdateAccess(AccessorDeclarationSyntax accessor);
}