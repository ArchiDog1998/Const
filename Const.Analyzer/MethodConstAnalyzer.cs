using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Const.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodConstAnalyzer : BaseConstAnalyzer
{
    protected override SyntaxKind Kind => SyntaxKind.MethodDeclaration;

    protected override void CheckMethod(SyntaxNodeAnalysisContext context, IMethodSymbol symbol, SyntaxNode body)
    {
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
                ReportMember(context, name);
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
                ReportMethod(context, name);
            }
        }
    }
}