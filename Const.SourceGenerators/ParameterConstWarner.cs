using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Const.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class ParameterConstWarner : BaseConstWarner<ParameterSyntax>
{
    protected override void Execute(SourceProductionContext context,
        ImmutableArray<(ParameterSyntax TargetNode, SemanticModel SemanticModel)> data)
    {
        var result = data.GroupBy(p =>
        {
            var node = p.TargetNode.GetParent<LocalFunctionStatementSyntax>() as SyntaxNode 
            ?? p.TargetNode.GetParent<BaseMethodDeclarationSyntax>();
            return node;
        });

        foreach (var pair in result)
        {
            var body = GetMethodBody(pair.Key);
            if (body is null) continue;

            List<string> selfNames = [], memberNames = [], memberInMemberNames = [];

            foreach (var (paramNode, paramModel) in pair)
            {
                var paramName = paramNode.Identifier.ToFullString();

                var type = GetConstTypeAttribute(paramModel.GetDeclaredSymbol(paramNode));

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
                    DontModifyWarning(context, name, "parameter");
                }
            }
        }
    }
}