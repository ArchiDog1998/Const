using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Const.Analyzer;

public abstract class BaseConstAnalyzer : DiagnosticAnalyzer
{
    #region Const String

    private const string ConstName = "Const.ConstAttribute";

    private const string DiagnosticId = "CT0001";
    private const string Title = "Don't modify it";
    private const string Category = "Usage";

    private const string ParameterDescriptorDescription = "Don't modify this parameter";
    private const string MethodDescriptorDescription = "Don't invoke this method";
    private const string MemberDescriptorDescription = "Don't modify this member";

    #endregion

    #region Static

    private static readonly DiagnosticDescriptor ParameterDescriptor = new (DiagnosticId, Title, 
        ParameterDescriptorDescription,
        Category, DiagnosticSeverity.Error, true, 
        ParameterDescriptorDescription);
    
    private static readonly DiagnosticDescriptor MethodDescriptor = new (DiagnosticId, Title, 
        MethodDescriptorDescription,
        Category, DiagnosticSeverity.Error, true, 
        MethodDescriptorDescription);
    
    private static readonly DiagnosticDescriptor MemberDescriptor = new (DiagnosticId, Title, 
        MemberDescriptorDescription,
        Category, DiagnosticSeverity.Error, true, 
        MemberDescriptorDescription);
    
    protected static void ReportMember(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
    {
        context.ReportDiagnostic(Diagnostic.Create(MemberDescriptor, syntaxNode.GetLocation()));
    }
    private static void ReportParameter(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
    {
        context.ReportDiagnostic(Diagnostic.Create(ParameterDescriptor, syntaxNode.GetLocation()));
    }
    
    protected static void ReportMethod(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
    {
        context.ReportDiagnostic(Diagnostic.Create(MethodDescriptor, syntaxNode.GetLocation()));
    }
    
    protected static byte GetConstTypeAttribute(ISymbol? symbol)
    {
        var attr = symbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.GetFullMetadataName() is ConstName);
        if (attr == null) return 0;
        var type = attr?.NamedArguments.FirstOrDefault(p => p.Key == "Type").Value;
        return (byte?)type?.Value ?? byte.MaxValue;
    }
    
    protected static  ISymbol[] AccessibleFieldsAndProperties(INamedTypeSymbol? typeSymbol)
    {
        return AccessibleMembers(typeSymbol, GetFieldsAnProperties);

        static IEnumerable<ISymbol> GetFieldsAnProperties(INamedTypeSymbol typeSymbol) => typeSymbol.GetMembers().Where(s =>
        {
            if (s is IFieldSymbol) return true;
            if (s is IPropertySymbol) return true;
            return false;
        });
    }

    protected static ISymbol[] AccessibleMethods(INamedTypeSymbol? typeSymbol)
    {
        return AccessibleMembers(typeSymbol, GetMethods);

        static IEnumerable<ISymbol> GetMethods(INamedTypeSymbol typeSymbol) => typeSymbol.GetMembers().Where(s =>
        {
            if (s is IMethodSymbol) return true;
            return false;
        });
    }

    private static ISymbol[] AccessibleMembers(INamedTypeSymbol? typeSymbol, Func<INamedTypeSymbol, IEnumerable<ISymbol>> getMembers)
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
    
    protected static bool HasFlag(byte value, byte flag) => (value & flag) == flag;
  
    protected static SimpleNameSyntax? GetFirstAccessorName(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment, bool containThis, out int deep, out bool isThis)
    {
        return GetFirstAccessorName(context, assignment.Left, containThis, out deep, out isThis);
    }
    protected static SimpleNameSyntax? GetFirstAccessorName(SyntaxNodeAnalysisContext context, ExpressionSyntax exp, bool containThis, out int deep, out bool isThis)
    {
        deep = 0;
        isThis = false;

        while (exp is not SimpleNameSyntax)
        {
            deep++;
            if (exp is MemberAccessExpressionSyntax member)
            {
                exp = member.Expression;
            }
            else if (containThis && exp is ThisExpressionSyntax thisExp)
            {
                if (thisExp.Parent is MemberAccessExpressionSyntax m)
                {
                    exp = m.Name;
                    deep -= 2;
                    isThis = true;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        if (exp is SimpleNameSyntax name) return name;

        var desc = new DiagnosticDescriptor("CT101", "Where is it?", $"How to access this identifier name in Expression?", "ToolBug", DiagnosticSeverity.Warning,
            true);
        var diagnostic = Diagnostic.Create(desc, exp.GetLocation());
        context.ReportDiagnostic(diagnostic);

        return null;
    }
    
    protected static string GetSyntaxName(SimpleNameSyntax name) => name.Identifier.ToFullString().Trim();

    #endregion

    protected abstract SyntaxKind Kind { get; }

    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ParameterDescriptor, MethodDescriptor);
    
    public sealed override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeNode, Kind);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var model = context.SemanticModel;
        
        var body = GetMethodBody(context.Node);
        if (body is null) return;
        
        if (model.GetDeclaredSymbol(context.Node) is not IMethodSymbol symbol) return;

        CheckMethod(context, symbol, body);
        CheckParameter(context, symbol, body);
        
        return;

        static SyntaxNode? GetMethodBody(SyntaxNode? method) => method switch
        {
            LocalFunctionStatementSyntax func => func.Body as SyntaxNode ?? func.ExpressionBody,
            BaseMethodDeclarationSyntax m => m.Body as SyntaxNode ?? m.ExpressionBody,
            _ => null
        };
        
        static void CheckParameter(SyntaxNodeAnalysisContext context, IMethodSymbol symbol, SyntaxNode body)
        {
            List<string> selfNames = [], memberNames = [], memberInMemberNames = [];

            foreach (var parameter in symbol.Parameters)
            {
                var paramName = parameter.Name;
                var type = GetConstTypeAttribute(parameter);

                if (HasFlag(type, 1 << 0))
                {
                    selfNames.Add(paramName);
                }

                if (HasFlag(type, 1 << 1))
                {
                    memberNames.Add(paramName);
                }

                if (HasFlag(type, 1 << 2))
                {
                    memberInMemberNames.Add(paramName);
                }
            }
        
            foreach (var statement in body.GetChildren<AssignmentExpressionSyntax>())
            {
                var name = GetFirstAccessorName(context, statement, false, out var deep, out _);
                if (name is null) continue;

                var left = GetSyntaxName(name);

                if (deep switch
                    {
                        0 => selfNames.Contains(left),
                        1 => memberNames.Contains(left),
                        _ => memberInMemberNames.Contains(left),
                    })
                {
                    ReportParameter(context, name);
                }
            }
        }
    }

    protected abstract void CheckMethod(SyntaxNodeAnalysisContext context, IMethodSymbol symbol, SyntaxNode body);
}