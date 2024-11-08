using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Const.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]

public class FunctionConstAnalyzer: BaseConstAnalyzer
{
    protected override SyntaxKind Kind => SyntaxKind.LocalFunctionStatement;
    protected override void CheckMethod(SyntaxNodeAnalysisContext context, IMethodSymbol symbol, SyntaxNode body)
    {
    }
}