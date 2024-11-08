using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Const.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DeclarationConstAnalyzer : DiagnosticAnalyzer
{
    private const string ConstName = "Const.ConstAttribute";

    #region Diagnotic Warning

    private const string DiagnosticId = "CT1001";
    private const string Title = "Don't modify it";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor ParameterDescriptor = new(DiagnosticId, Title,
        "Don't modify this parameter",
        Category, DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor MethodDescriptor = new(DiagnosticId, Title,
        "Don't invoke this method",
        Category, DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor MemberDescriptor = new(DiagnosticId, Title,
        "Don't modify this member",
        Category, DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor CantFindDescriptor = new("CT2001", "Where is it?",
        $"How to access this identifier name in Expression?", "ToolBug", DiagnosticSeverity.Warning,
        true);

    private static void ReportMember(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
    {
        context.ReportDiagnostic(Diagnostic.Create(MemberDescriptor, syntaxNode.GetLocation()));
    }

    private static void ReportParameter(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
    {
        context.ReportDiagnostic(Diagnostic.Create(ParameterDescriptor, syntaxNode.GetLocation()));
    }

    private static void ReportMethod(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
    {
        context.ReportDiagnostic(Diagnostic.Create(MethodDescriptor, syntaxNode.GetLocation()));
    }

    private static void ReportNotFound(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
    {
        context.ReportDiagnostic(Diagnostic.Create(CantFindDescriptor, syntaxNode.GetLocation()));
    }

    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [ParameterDescriptor, MethodDescriptor, MemberDescriptor, CantFindDescriptor];

    #endregion

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

        if (model.GetDeclaredSymbol(context.Node) is not IMethodSymbol symbol)
        {
            return;
        }

        var type = GetConstTypeAttribute(symbol);
        CheckMember(context, symbol, body, type);
        CheckMethod(context, symbol, body, type);

        CheckParameter(context, symbol, body);
    }

    private static byte GetConstTypeAttribute(IMethodSymbol symbol)
    {
        byte result = 0;

        var methodSymbol = symbol;
        do
        {
            result |= GetConstTypeAttributeRaw(methodSymbol);
            methodSymbol = methodSymbol.OverriddenMethod;
        } while (methodSymbol is not null);

        return result;
    }

    private static byte GetConstTypeAttribute(IMethodSymbol symbol, int index)
    {
        byte result = 0;

        var methodSymbol = symbol;
        do
        {
            result |= GetConstTypeAttributeRaw(methodSymbol.Parameters[index]);
            methodSymbol = methodSymbol.OverriddenMethod;
        } while (methodSymbol is not null);

        return result;
    }

    private static byte GetConstTypeAttributeRaw(ISymbol? symbol)
    {
        var attr = symbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.GetFullMetadataName() is ConstName);
        if (attr == null) return 0;
        var type = attr?.NamedArguments.FirstOrDefault(p => p.Key == "Type").Value;
        return (byte?)type?.Value ?? byte.MaxValue;
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
            IMethodSymbol symbol)
        {
            List<string> selfNames = [], memberNames = [], memberInMemberNames = [];

            for (var i = 0; i < symbol.Parameters.Length; i++)
            {
                var paramName = symbol.Parameters[i].Name;
                var type = GetConstTypeAttribute(symbol, i);

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

            return (selfNames, memberNames, memberInMemberNames);
        }

        void CheckOneLocalFunction(LocalFunctionStatementSyntax localFunction, IEnumerable<string> skipNames)
        {
            var body = GetMethodBody(localFunction);
            if (body is null) return;
            skipNames = skipNames.Union(GetParameterNames(localFunction));
            CheckAssignment(body, skipNames);

            foreach (var local in body.GetChildren<LocalFunctionStatementSyntax>())
            {
                CheckOneLocalFunction(local, skipNames);
            }

            return;

            IEnumerable<string> GetParameterNames(LocalFunctionStatementSyntax statement)
            {
                return statement.ParameterList.Parameters.Select(p => p.Identifier.Text.Trim());
            }
        }

        void CheckAssignment(SyntaxNode body, IEnumerable<string> skipNames)
        {
            CheckChildren(context, body, false, (name, deep, isThis) =>
            {
                var left = GetSyntaxName(name);

                if (skipNames.Contains(left)) return;

                if (deep switch
                    {
                        0 => selfNames.Contains(left),
                        1 => memberNames.Contains(left),
                        _ => memberInMemberNames.Contains(left),
                    })
                {
                    ReportParameter(context, name);
                }
            });
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
    private static void CheckMember(SyntaxNodeAnalysisContext context, IMethodSymbol symbol, SyntaxNode body, byte type)
    {
        var exceptions = symbol.Parameters.Select(p => p.Name);

        var members = AccessibleFieldsAndProperties(symbol.ContainingType)
            .Select(s => s.Name);

        CheckChildren(context, body, true, (name, deep, isThis) =>
        {
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
        });

        return;

        static ISymbol[] AccessibleFieldsAndProperties(INamedTypeSymbol? typeSymbol)
        {
            return AccessibleMembers(typeSymbol, GetFieldsAnProperties);

            static IEnumerable<ISymbol> GetFieldsAnProperties(INamedTypeSymbol typeSymbol) => typeSymbol.GetMembers()
                .Where(
                    s =>
                    {
                        if (s is IFieldSymbol) return true;
                        if (s is IPropertySymbol) return true;
                        return false;
                    });
        }
    }

    private static void CheckChildren(SyntaxNodeAnalysisContext context, SyntaxNode body, bool containThis,
        Action<SimpleNameSyntax, int, bool> operation)
    {
        foreach (var statement in body.GetChildren<AssignmentExpressionSyntax>(n => n is LocalFunctionStatementSyntax))
        {
            var name = GetFirstAccessorNameAssignment(context, statement, containThis, out var deep, out var isThis);
            if (name is null) continue;
            operation(name, deep, isThis);
            continue;

            static SimpleNameSyntax? GetFirstAccessorNameAssignment(SyntaxNodeAnalysisContext context,
                AssignmentExpressionSyntax assignment, bool containThis, out int deep, out bool isThisOrBase)
            {
                return GetFirstAccessorName(context, assignment.Left, containThis, out deep, out isThisOrBase);
            }
        }

        foreach (var statement in
                 body.GetChildren<PostfixUnaryExpressionSyntax>(n => n is LocalFunctionStatementSyntax))
        {
            var name = GetFirstAccessorNamePost(context, statement, containThis, out var deep, out var isThis);
            if (name is null) continue;
            operation(name, deep, isThis);
            continue;

            static SimpleNameSyntax? GetFirstAccessorNamePost(SyntaxNodeAnalysisContext context,
                PostfixUnaryExpressionSyntax assignment, bool containThis, out int deep, out bool isThisOrBase)
            {
                return GetFirstAccessorName(context, assignment.Operand, containThis, out deep, out isThisOrBase);
            }
        }

        foreach (var statement in body.GetChildren<PrefixUnaryExpressionSyntax>(n => n is LocalFunctionStatementSyntax))
        {
            var name = GetFirstAccessorNamePrefix(context, statement, containThis, out var deep, out var isThis);
            if (name is null) continue;
            operation(name, deep, isThis);
            continue;

            static SimpleNameSyntax? GetFirstAccessorNamePrefix(SyntaxNodeAnalysisContext context,
                PrefixUnaryExpressionSyntax assignment, bool containThis, out int deep, out bool isThisOrBase)
            {
                return GetFirstAccessorName(context, assignment.Operand, containThis, out deep, out isThisOrBase);
            }
        }
    }

    private static void CheckMethod(SyntaxNodeAnalysisContext context, IMethodSymbol symbol, SyntaxNode body, byte type)
    {
        var localFunctions = GetLocalFunctions(context);
        var localFunctionNames = localFunctions.Select(f => f.Name);
        var cantLocalFunctionNames = localFunctions.Where(CantInvokeMethod).Select(s => s.Name);
        var cantMethodsNames = AccessibleMethods(symbol.ContainingType).Where(CantInvokeMethod).Select(s => s.Name);

        foreach (var statement in body.GetChildren<InvocationExpressionSyntax>())
        {
            var name = GetFirstAccessorNameInvoke(context, statement, true, out var deep, out var isThis);

            if (name is null) continue;

            var left = GetSyntaxName(name);

            if (!isThis && localFunctionNames.Contains(left))
            {
                if (cantLocalFunctionNames.Contains(left))
                {
                    ReportMethod(context, name);
                }
            }
            else
            {
                if (cantMethodsNames.Contains(left))
                {
                    ReportMethod(context, name);
                }
            }
        }

        return;

        static SimpleNameSyntax? GetFirstAccessorNameInvoke(SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax assignment, bool containThis, out int deep, out bool isThisOrBase)
        {
            return GetFirstAccessorName(context, assignment.Expression, containThis, out deep, out isThisOrBase);
        }

        static IMethodSymbol[] AccessibleMethods(INamedTypeSymbol? typeSymbol)
        {
            return AccessibleMembers(typeSymbol, GetMethods);

            static IEnumerable<IMethodSymbol> GetMethods(INamedTypeSymbol typeSymbol) =>
                typeSymbol.GetMembers().OfType<IMethodSymbol>();
        }

        static IMethodSymbol[] GetLocalFunctions(SyntaxNodeAnalysisContext context)
        {
            List<IMethodSymbol> result = [];

            var node = context.Node;

            var parent = node.Parent.GetParent<LocalFunctionStatementSyntax>();

            while (parent is not null)
            {
                AddResult(parent);
                parent = parent.Parent.GetParent<LocalFunctionStatementSyntax>();
            }

            if (node.Parent.GetParent<BaseMethodDeclarationSyntax>() is { } baseMethodNode)
            {
                AddResult(baseMethodNode);
            }

            return result.ToArray();

            void AddResult(SyntaxNode methodNode)
            {
                foreach (var local in methodNode.GetChildren<LocalFunctionStatementSyntax>())
                {
                    if (result.Any(s => s.Name.Trim() == local.Identifier.Text.Trim())) continue;
                    var func = context.SemanticModel.GetDeclaredSymbol(local);
                    if (func is null) continue;
                    result.Add(func);
                }
            }
        }

        bool CantInvokeMethod(IMethodSymbol symbol)
        {
            var methodType = GetConstTypeAttribute(symbol);
            if (HasFlag(type, 1 << 0) && !HasFlag(methodType, 1 << 0)) return true;
            if (HasFlag(type, 1 << 1) && !HasFlag(methodType, 1 << 1)) return true;
            if (HasFlag(type, 1 << 2) && !HasFlag(methodType, 1 << 2)) return true;
            return false;
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

    private static bool HasFlag(byte value, byte flag) => (value & flag) == flag;


    private static SimpleNameSyntax? GetFirstAccessorName(SyntaxNodeAnalysisContext context, ExpressionSyntax exp,
        bool containThis, out int deep, out bool isThisOrBase)
    {
        deep = 0;
        isThisOrBase = false;

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
                    isThisOrBase = true;
                }
                else
                {
                    break;
                }
            }
            else if (containThis && exp is BaseExpressionSyntax baseExp)
            {
                if (baseExp.Parent is MemberAccessExpressionSyntax m)
                {
                    exp = m.Name;
                    deep -= 2;
                    isThisOrBase = true;
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

        ReportNotFound(context, exp);
        return null;
    }

    private static string GetSyntaxName(SimpleNameSyntax name) => name.Identifier.ToFullString().Trim();
}