using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Const.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class MethodConstWarner : BaseConstWarner<BaseMethodDeclarationSyntax>
{
    protected override void Execute(SourceProductionContext context, ImmutableArray<(BaseMethodDeclarationSyntax TargetNode, SemanticModel SemanticModel)> array)
    {
        foreach (var (node, model) in array)
        {
            if (model.GetDeclaredSymbol(node) is not IMethodSymbol symbol) continue;
            var type = GetConstTypeAttribute(symbol);

            var exceptions = symbol.Parameters.Select(p => p.Name);
            var members = AccessibleFieldsAndProperties(symbol.ContainingType)
                .Select(s => s.Name);
            var methods = AccessibleMethods(symbol.ContainingType).Where(s =>
            {
                var methodType = GetConstTypeAttribute(s);
                if (HasFlag(type, 1 << 0) && !HasFlag(methodType, 1 << 0)) return true;
                if (HasFlag(type, 1 << 1) && !HasFlag(methodType, 1 << 1)) return true;
                if (HasFlag(type, 1 << 2) && !HasFlag(methodType, 1 << 2)) return true;
                return false;
            }).Select(s => s.Name);


            var body = GetMethodBody(node);
            if (body is null) continue;

            foreach (var statement in body.GetChildren<AssignmentExpressionSyntax>())
            {
                var name = GetFirstAccessorName(context, statement, true, out var deep, out var isThis);
                if (name is null) continue;

                var left = GetSyntaxName(name);

                if (members.Contains(left) 
                    && (isThis || !exceptions.Contains(left))
                    && deep switch
                {
                    0 => HasFlag(type, 1 << 0),
                    1 => HasFlag(type, 1 << 1),
                    _ => HasFlag(type, 1 << 2),
                })
                {
                    DontModifyWarning(context, name, "member");
                }
            }

            foreach (var statement in body.GetChildren<InvocationExpressionSyntax>())
            {
                var name = GetFirstAccessorName(context, statement.Expression, true, out var deep, out var isThis);

                if (name is null) continue;

                var left = GetSyntaxName(name);

                if (methods.Contains(left)
                    && (isThis || !exceptions.Contains(left)))
                {
                    DontInvokeWarning(context, name);
                }
            }
        }
    }

    private ISymbol[] AccessibleFieldsAndProperties(INamedTypeSymbol? typeSymbol)
    {
        return AccessibleMembers(typeSymbol, GetFieldsAnProperties);

        static IEnumerable<ISymbol> GetFieldsAnProperties(INamedTypeSymbol typeSymbol) => typeSymbol.GetMembers().Where(s =>
        {
            if (s is IFieldSymbol) return true;
            if (s is IPropertySymbol) return true;
            return false;
        });
    }

    private ISymbol[] AccessibleMethods(INamedTypeSymbol? typeSymbol)
    {
        return AccessibleMembers(typeSymbol, GetMethods);

        static IEnumerable<ISymbol> GetMethods(INamedTypeSymbol typeSymbol) => typeSymbol.GetMembers().Where(s =>
        {
            if (s is IMethodSymbol) return true;
            return false;
        });
    }

    private ISymbol[] AccessibleMembers(INamedTypeSymbol? typeSymbol, Func<INamedTypeSymbol, IEnumerable<ISymbol>> getMembers)
    {
        if (typeSymbol == null) return [];
        var contains = typeSymbol.ContainingAssembly;

        var allSymbols = getMembers(typeSymbol);

        typeSymbol = typeSymbol.BaseType;

        while(typeSymbol != null)
        {
            allSymbols = allSymbols.Union(getMembers(typeSymbol).Where(s =>
            {
                var access = s.DeclaredAccessibility;

                if (s.ContainingAssembly.Equals(contains, SymbolEqualityComparer.Default))
                {
                    return access
                        is Accessibility.Public
                        or Accessibility.Protected
                        or Accessibility.Internal
                        or Accessibility.ProtectedAndInternal;
                }
                else
                {
                    return access
                        is Accessibility.Public
                        or Accessibility.Protected;
                }
            }), SymbolEqualityComparer.Default);

            typeSymbol = typeSymbol.BaseType;
        }

        return allSymbols.ToArray();
    }
}
