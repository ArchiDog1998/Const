using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Const.Analyzer;

[Generator(LanguageNames.CSharp)]
public class FunctionConstWarner : BaseConstWarner<LocalFunctionStatementSyntax>
{
    protected override void Execute(SourceProductionContext context, ImmutableArray<(LocalFunctionStatementSyntax TargetNode, SemanticModel SemanticModel)> array)
    {
    }
}
