using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ArchiToolkit.Analyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DeclarationConstAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeName = "ArchiToolkit.ConstAttribute";

    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        DiagnosticExtensions.ConstDescriptors;

    public sealed override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                               GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement,
            SyntaxKind.LocalDeclarationStatement,
            SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration, SyntaxKind.InitAccessorDeclaration,
            SyntaxKind.AddAccessorDeclaration, SyntaxKind.RemoveAccessorDeclaration,
            SyntaxKind.UnknownAccessorDeclaration
        );
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var model = context.SemanticModel;

        var body = GetMethodBody(context.Node);
        if (body is null) return;

        if (model.GetDeclaredSymbol(context.Node) is not IMethodSymbol symbol) return;

        var type = GetConstTypeAttribute(symbol);
        CheckMember(context, symbol, body, type);
        CheckMethod(context, body, type);
        
        CheckParameter(context, symbol, body);
    }

    private static ConstType GetConstTypeAttribute(IMethodSymbol symbol)
    {
        ConstType result = 0;

        var methodSymbol = symbol;
        do
        {
            result |= GetConstTypeAttributeRaw(methodSymbol);
            methodSymbol = methodSymbol.OverriddenMethod;
        } while (methodSymbol is not null);

        return result;
    }

    private static ConstType GetConstTypeAttribute(IMethodSymbol symbol, int index)
    {
        ConstType result = 0;

        var methodSymbol = symbol;
        do
        {
            result |= GetConstTypeAttributeRaw(methodSymbol.Parameters[index]);
            methodSymbol = methodSymbol.OverriddenMethod;
        } while (methodSymbol is not null);

        return result;
    }

    private static ConstType GetConstTypeAttributeRaw(ISymbol? symbol)
    {
        var attr = symbol?.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.GetFullMetadataName() is AttributeName);
        if (attr == null) return 0;
        var type = attr.NamedArguments.FirstOrDefault(p => p.Key == "Type").Value;
        var by = (ConstType?)type.Value ?? ConstType.All;
        return by;
    }

    private static SyntaxNode? GetMethodBody(SyntaxNode? method) => method switch
    {
        LocalFunctionStatementSyntax func => func.Body as SyntaxNode ?? func.ExpressionBody,
        BaseMethodDeclarationSyntax m => m.Body as SyntaxNode ?? m.ExpressionBody,
        AccessorDeclarationSyntax ac => ac.Body as SyntaxNode ?? ac.ExpressionBody,
        _ => null
    };

    private static void CheckParameter(SyntaxNodeAnalysisContext context, IMethodSymbol symbol, SyntaxNode body)
    {
        var (selfNames, memberNames, memberInMemberNames) = GetParameterNames(symbol);
        CheckAssignment(body, []);
        foreach (var local in body.GetChildren<LocalFunctionStatementSyntax>())
        {
            CheckOneLocalFunction(local, []);
        }

        return;

        (List<string> selfNames, List<string> memberNames, List<string> memberInMemberNames) GetParameterNames(
            IMethodSymbol method)
        {
            List<string> mSelfNames = [], mMemberNames = [], mMemberInMemberNames = [];

            for (var i = 0; i < method.Parameters.Length; i++)
            {
                var paramName = method.Parameters[i].Name;
                var type = GetConstTypeAttribute(method, i);

                if (type.HasFlag(ConstType.Self))
                {
                    mSelfNames.Add(paramName);
                }

                if (type.HasFlag(ConstType.Members))
                {
                    mMemberNames.Add(paramName);
                }

                if (type.HasFlag(ConstType.MembersInMembers))
                {
                    mMemberInMemberNames.Add(paramName);
                }
            }

            return (mSelfNames, mMemberNames, mMemberInMemberNames);
        }

        void CheckOneLocalFunction(LocalFunctionStatementSyntax localFunction, IEnumerable<string> skipNames)
        {
            var localBody = GetMethodBody(localFunction);
            if (localBody is null) return;
            var addedSkipNames = skipNames.Union(GetLocalParameterNames(localFunction)).ToArray();
            CheckAssignment(localBody, addedSkipNames);

            foreach (var local in localBody.GetChildren<LocalFunctionStatementSyntax>())
            {
                CheckOneLocalFunction(local, addedSkipNames);
            }

            return;

            IEnumerable<string> GetLocalParameterNames(LocalFunctionStatementSyntax statement)
            {
                return statement.ParameterList.Parameters.Select(p => p.Identifier.Text.Trim());
            }
        }

        void CheckAssignment(SyntaxNode subBody, IEnumerable<string> skipNames)
        {
            CheckChildren(context, subBody, false, (name, deep, _) =>
            {
                var left = GetSyntaxName(name);

                if (skipNames.Contains(left)) return 0;

                return deep switch
                {
                    0 => selfNames.Contains(left) ? ConstType.Self : 0,
                    1 => memberNames.Contains(left) ? ConstType.Members : 0,
                    _ => memberInMemberNames.Contains(left) ? ConstType.MembersInMembers : 0,
                };
            }, DiagnosticExtensions.ReportParameter, DiagnosticExtensions.ReportParameterInvoke);
        }
    }

    /// <summary>
    /// Check if it is accessing the wrong field or the wrong property.
    /// Just check the main declaration without the sub local functions.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="symbol"></param>
    /// <param name="body"></param>
    /// <param name="type"></param>
    private static void CheckMember(SyntaxNodeAnalysisContext context, IMethodSymbol symbol, SyntaxNode body,
        ConstType type)
    {
        var exceptions = symbol.Parameters.Select(p => p.Name);

        var members = AccessibleFieldsAndProperties(symbol.ContainingType)
            .Select(s => s.Name);

        CheckChildren(context, body, true, (name, deep, isThis) =>
        {
            var left = GetSyntaxName(name);

            if (!members.Contains(left)) return 0;
            if (!(isThis || !exceptions.Contains(left))) return 0;

            return deep switch
            {
                0 => type.HasFlag(ConstType.Self) ? ConstType.Self : 0,
                1 => type.HasFlag(ConstType.Members) ? ConstType.Members : 0,
                _ => type.HasFlag(ConstType.MembersInMembers) ? ConstType.MembersInMembers : 0,
            };
        }, DiagnosticExtensions.ReportMember, DiagnosticExtensions.ReportMemberInvoke);

        return;

        static ISymbol[] AccessibleFieldsAndProperties(INamedTypeSymbol? typeSymbol)
        {
            return AccessibleMembers(typeSymbol, GetFieldsAnProperties);

            static IEnumerable<ISymbol> GetFieldsAnProperties(INamedTypeSymbol typeSymbol) => typeSymbol.GetMembers()
                .Where(s => s is IFieldSymbol or IPropertySymbol);
        }
    }

    private static void CheckChildren(SyntaxNodeAnalysisContext context, SyntaxNode body, bool containThis,
        Func<SimpleNameSyntax, int, bool, ConstType> shouldReport,
        Action<SyntaxNodeAnalysisContext, SyntaxNode, ConstType> reportAction,
        Action<SyntaxNodeAnalysisContext, SyntaxNode, ConstType, string> reportMethodAction)
    {
        CheckChildrenSyntax<AssignmentExpressionSyntax>(s => GetAssignmentExpressionSet(s).ToArray());
        CheckChildrenSyntax<PostfixUnaryExpressionSyntax>(s => [s.Operand]);
        CheckChildrenSyntax<PrefixUnaryExpressionSyntax>(s => [s.Operand]);
        CheckChildrenSyntax<InvocationExpressionSyntax>(s => [s.Expression]);

        return;

        static IEnumerable<ExpressionSyntax> GetAssignmentExpressionSet(
            AssignmentExpressionSyntax assignmentExpressionSyntax)
        {
            if (assignmentExpressionSyntax.Right is not AssignmentExpressionSyntax right)
                return [assignmentExpressionSyntax.Left];

            return [assignmentExpressionSyntax.Left, ..GetAssignmentExpressionSet(right)];
        }

        void CheckChildrenSyntax<T>(Func<T, ExpressionSyntax[]> getExpression) where T : SyntaxNode
        {
            foreach (var statement in body.GetChildren<T>(n => n is LocalFunctionStatementSyntax))
            {
                foreach (var expression in getExpression(statement))
                {
                    CheckExpression(expression, statement);
                }
            }

            return;

            void CheckExpression(ExpressionSyntax expression, T statement)
            {
                var name = GetFirstAccessorName(context, expression, containThis, out var deep,
                    out var isThis);
                if (name is null) return;

                var nameSymbol = context.SemanticModel.GetSymbolInfo(name).Symbol;
                if (nameSymbol is not IPropertySymbol and not IFieldSymbol and not IParameterSymbol) return;

                int[] deeps = [deep];
                var methodName = string.Empty;
                if (statement is InvocationExpressionSyntax invocation
                    && context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol is
                        IMethodSymbol methodSymbol)
                {
                    methodName = methodSymbol.Name;
                    //TODO: it is better to check if the assembly has the dependency about this assembly.
                    if (!methodSymbol.ContainingAssembly.Equals(nameSymbol.ContainingAssembly,
                            SymbolEqualityComparer.Default))
                    {
                        // Skip this method. We can't edit it.
                        return;
                    }

                    var edition = GetConstTypeAddition(GetConstTypeAttribute(methodSymbol));

                    if (edition.Length == 0)
                    {
                        //We don't need to error it, it is const always.
                        return;
                    }

                    deeps = [..edition.Select(i => i + deep)];
                }

                var type = deeps.Aggregate<int, ConstType>(0, (current, d) => current | shouldReport(name, d, isThis));

                if (type is 0) return;

                if (string.IsNullOrEmpty(methodName))
                {
                    reportAction(context, name, type);
                }
                else
                {
                    reportMethodAction(context, name, type, methodName);
                }
            }

            static int[] GetConstTypeAddition(ConstType type)
            {
                List<int> result = new(3);
                if (!type.HasFlag(ConstType.MembersInMembers))
                {
                    result.Add(2);
                }

                if (!type.HasFlag(ConstType.Members))
                {
                    result.Add(1);
                }

                if (!type.HasFlag(ConstType.Self))
                {
                    result.Add(0);
                }

                return result.ToArray();
            }
        }
    }

    private static void CheckMethod(SyntaxNodeAnalysisContext context, SyntaxNode body, ConstType type)
    {
        foreach (var statement in body.GetChildren<InvocationExpressionSyntax>())
        {
            var name = GetFirstAccessorNameInvoke(context, statement, true, out _);
            if (name is null) continue;
            
            if (context.SemanticModel.GetSymbolInfo(statement.Expression).Symbol is not IMethodSymbol methodSymbol)
                continue;
            
            if (context.SemanticModel.GetSymbolInfo(name).Symbol is not IMethodSymbol)
                continue;
            
            if (!CantInvokeMethod(methodSymbol)) continue;
            context.ReportMethod(name, type);
        }

        return;

        bool CantInvokeMethod(IMethodSymbol methodSymbol)
        {
            var methodType = GetConstTypeAttribute(methodSymbol);
            if (CheckFlag(ConstType.Self)) return true;
            if (CheckFlag(ConstType.Members)) return true;
            if (CheckFlag(ConstType.MembersInMembers)) return true;
            return false;

            bool CheckFlag(ConstType item) => type.HasFlag(item) && !methodType.HasFlag(item);
        }
    }

    private static T[] AccessibleMembers<T>(INamedTypeSymbol? typeSymbol,
        Func<INamedTypeSymbol, IEnumerable<T>> getMembers) where T : ISymbol
    {
        if (typeSymbol == null) return [];
        var contains = typeSymbol.ContainingAssembly;

        var allSymbols = getMembers(typeSymbol);

        typeSymbol = typeSymbol.BaseType;

        while (typeSymbol != null)
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
            }));

            typeSymbol = typeSymbol.BaseType;
        }

        return allSymbols.ToArray();
    }

    private static SimpleNameSyntax? GetFirstAccessorNameInvoke(SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax assignment, bool containThis, out int deep)
    {
        return GetFirstAccessorName(context, assignment.Expression, containThis, out deep, out _);
    }

    private static SimpleNameSyntax? GetFirstAccessorName(SyntaxNodeAnalysisContext context, ExpressionSyntax exp,
        bool containThis, out int deep, out bool isThisOrBase)
    {
        deep = 0;
        isThisOrBase = false;
        
        while (true)
        {
            deep++;
            
            switch (exp)
            {
                case MemberAccessExpressionSyntax member:
                    exp = member.Expression;
                    break;

                case ThisExpressionSyntax:
                case BaseExpressionSyntax:
                    if (containThis && exp.Parent is MemberAccessExpressionSyntax m)
                    {
                        exp = m.Name;
                        deep -= 2;
                        isThisOrBase = true;
                    }
                    else
                    {
                        return null;
                    }

                    break;
                
                case SimpleNameSyntax name:
                    return name;
                
                case AwaitExpressionSyntax await:
                    exp = await.Expression;
                    break;
                
                case InvocationExpressionSyntax invocation:
                    exp =invocation.Expression;
                    break;
                
                default:
                    context.ReportCantFind(exp);
                    return null;
            }
        }
    }

    private static string GetSyntaxName(SimpleNameSyntax name) => name.Identifier.ToFullString().Trim();
}