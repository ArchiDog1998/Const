using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SimpleRoslynHelper;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Const.SourceGenerators;

public abstract class BaseConstWarner<T> : IIncrementalGenerator where T : SyntaxNode
{
    protected const string CONST_NAME = "Const.ConstAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName
            (CONST_NAME,
                static (node, _) => node is T,
                static (n, ct) => ((T)n.TargetNode, n.SemanticModel))
            .Where(m => m.Item1 != null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    protected abstract void Execute(SourceProductionContext context, ImmutableArray<(T TargetNode, SemanticModel SemanticModel)> array);


    protected static SimpleNameSyntax? GetFirstAccessorName(SourceProductionContext context, AssignmentExpressionSyntax assignment, bool containThis, out int deep, out bool isThis)
    {
        return GetFirstAccessorName(context, assignment.Left, containThis, out deep, out isThis);
    }
    protected static SimpleNameSyntax? GetFirstAccessorName(SourceProductionContext context, ExpressionSyntax exp, bool containThis, out int deep, out bool isThis)
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

    protected static SyntaxNode? GetMethodBody(SyntaxNode? method)
    {
        SyntaxNode? body = null;

        switch (method)
        {
            case LocalFunctionStatementSyntax func:
                body = func.Body as SyntaxNode ?? func.ExpressionBody;
                break;

            case BaseMethodDeclarationSyntax m:
                body = m.Body as SyntaxNode ?? m.ExpressionBody;
                break;
        }

        return body;
    }

    protected static byte GetConstTypeAttribute(ISymbol? symbol)
    {
        var attr = symbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.GetFullMetadataName() is CONST_NAME);
        if (attr == null) return 0;
        var type = attr?.NamedArguments.FirstOrDefault(p => p.Key == "Type").Value;
        return (byte?)type?.Value ?? byte.MaxValue;
    }

    protected static bool HasFlag(byte value, byte flag) => (value & flag) == flag;

    protected static void DontModifyWarning(SourceProductionContext context, SimpleNameSyntax name, string type)
    {
        var left = GetSyntaxName(name);
        var desc = new DiagnosticDescriptor("CT0001", "No Modify", $"Don't Modify the {type} \'{left}\'.", "Problem", DiagnosticSeverity.Error,
true);
        var diagnostic = Diagnostic.Create(desc, name.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    protected static void DontInvokeWarning(SourceProductionContext context, SimpleNameSyntax name)
    {
        var left = GetSyntaxName(name);
        var desc = new DiagnosticDescriptor("CT0002", "No Modify", $"Don't invoke the method named \'{left}\'. It may modify the parameters.", "Problem", DiagnosticSeverity.Error,
true);
        var diagnostic = Diagnostic.Create(desc, name.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
