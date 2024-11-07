using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Const.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class FunctionConstWarner : BaseConstWarner<LocalFunctionStatementSyntax>
{
    protected override void Execute(SourceProductionContext context, ImmutableArray<(LocalFunctionStatementSyntax TargetNode, SemanticModel SemanticModel)> array)
    {
    }
}
