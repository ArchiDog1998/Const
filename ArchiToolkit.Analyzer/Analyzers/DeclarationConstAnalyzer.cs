﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ArchiToolkit.Analyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DeclarationConstAnalyzer : DiagnosticAnalyzer
{
    private const string
        ConstAttributeName = "ArchiToolkit.ConstAttribute",
        PureAttributeName = "System.Diagnostics.Contracts.PureAttribute";

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
        CheckPure(context, symbol, body, model, type);
    }

    private static void CheckPure(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol, SyntaxNode body, SemanticModel model,
        ConstType type)
    {
        if (!type.HasFlag(ConstType.Pure)) return;
        var symbols = body.GetChildren<SimpleNameSyntax>()
            .Select(name => (name, model.GetSymbolInfo(name).Symbol));
        foreach (var (name, calledSymbol) in symbols)
        {
            switch (calledSymbol)
            {
                case IMethodSymbol calledMethodSymbol when GetConstTypeAttribute(calledMethodSymbol) is not ConstType.Pure:
                    //TODO: it is better to check if the assembly has the dependency about this assembly.
                    context.ReportPureInvoke(name, methodSymbol.ContainingAssembly.Equals(calledMethodSymbol.ContainingAssembly,
                        SymbolEqualityComparer.Default));
                    break;

                case IPropertySymbol:
                case IFieldSymbol { IsConst: false }:
                    if (name.Parent is not MemberAccessExpressionSyntax memberGetter) break;
                    foreach (var item in GetFirstAccessorName(context, memberGetter, true))
                    {
                        var firstName = item.Name;
                        if (model.GetSymbolInfo(firstName).Symbol is IParameterSymbol) continue;
                        context.ReportPureMember(firstName);
                    }
                    break;
            }
        }
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
            var parents = assignmentExpressionSyntax.Parent?.AncestorsAndSelf().ToArray() ?? [];
            var inFirstInit = parents.OfType<InitializerExpressionSyntax>().Any()
                              && !parents.OfType<AssignmentExpressionSyntax>().Any();

            if (inFirstInit) //Skip the init check for the const.
            {
                return assignmentExpressionSyntax.Right is not AssignmentExpressionSyntax right ? [] : GetAssignmentExpressionSet(right);
            }
            else
            {
                if (assignmentExpressionSyntax.Right is not AssignmentExpressionSyntax right)
                    return [assignmentExpressionSyntax.Left];

                return [assignmentExpressionSyntax.Left, ..GetAssignmentExpressionSet(right)];
            }
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
                foreach (var item in GetFirstAccessorName(context, expression, containThis))
                {
                    var name = item.Name;
                    var deep = item.Deep;
                    var isThis = item.IsThisOrBase;
                    
                    var nameSymbol = context.SemanticModel.GetSymbolInfo(name).Symbol;
                    if (nameSymbol is not IPropertySymbol and not IFieldSymbol and not IParameterSymbol) return;

                    int[] deeps = [deep];
                    var methodName = string.Empty;
                    if (statement is InvocationExpressionSyntax invocation
                        && context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol is
                            IMethodSymbol methodSymbol)
                    {
                        methodName = methodSymbol.Name;
                        //TODO: it is better to check if the assembly has the dependency about this assembly. And the better way to warning for the things about how to 
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
            foreach (var name in GetFirstAccessorNameInvoke(context, statement, true))
            {
                if (context.SemanticModel.GetSymbolInfo(statement.Expression).Symbol is not IMethodSymbol methodSymbol)
                    continue;

                if (context.SemanticModel.GetSymbolInfo(name.Name).Symbol is not IMethodSymbol)
                    continue;

                if (!CantInvokeMethod(methodSymbol)) continue;
                context.ReportMethod(name.Name, type);
            }
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

    private static IReadOnlyList<AccessorName> GetFirstAccessorNameInvoke(SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax assignment, bool containThis)
    {
        return GetFirstAccessorName(context, assignment.Expression, containThis);
    }

    private readonly struct AccessorName(SimpleNameSyntax name, int deep, bool isThisOrBase)
    {
        public SimpleNameSyntax Name => name;
        public int Deep => deep;
        public bool IsThisOrBase => isThisOrBase;
    }

    private static IReadOnlyList<AccessorName> GetFirstAccessorName(SyntaxNodeAnalysisContext context, ExpressionSyntax exp,
        bool containThis)
    {
        var deep = 0;
        var isThisOrBase = false;

        while (true)
        {
            deep++;

            switch (exp)
            {
                case ConditionalAccessExpressionSyntax conditional:
                    exp = conditional.Expression;
                    break;
                case MemberAccessExpressionSyntax member:
                    exp = member.Expression;
                    break;
                case AwaitExpressionSyntax await:
                    exp = await.Expression;
                    break;
                case InvocationExpressionSyntax invocation:
                    exp = invocation.Expression;
                    break;
                case ParenthesizedExpressionSyntax parenthesized:
                    exp = parenthesized.Expression;
                    break;
                case TupleExpressionSyntax tuple:
                    return
                    [
                        ..tuple.Arguments.SelectMany(a => GetFirstAccessorName(context, a.Expression, containThis))
                    ];
                
                case ElementAccessExpressionSyntax elementAccess: //TODO: arguments?
                    exp = elementAccess.Expression;
                    break;
                // case PrefixUnaryExpressionSyntax prefixUnary:
                //     exp = prefixUnary.Operand;
                //     break;
                
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
                        return [];
                    }

                    break;

                case SimpleNameSyntax name:
                    return [new AccessorName(name, deep, isThisOrBase)];

                case DeclarationExpressionSyntax:
                case PredefinedTypeSyntax:
                case TypeOfExpressionSyntax:
                case QueryExpressionSyntax:
                case LiteralExpressionSyntax:
                case MemberBindingExpressionSyntax: //TODO: Shall we do sth with it?
                case BinaryExpressionSyntax:
                case BaseObjectCreationExpressionSyntax:
                case AnonymousObjectCreationExpressionSyntax:
                    return [];

                default:
                    context.ReportCantFind(exp);
                    return [];
            }
        }
    }

    private static string GetSyntaxName(SimpleNameSyntax name) => name.Identifier.ToFullString().Trim();

    private static ConstType GetConstTypeAttributeRaw(ISymbol? symbol)
    {
        var attrs = symbol?.GetAttributes();

        if (attrs?.Any(a => a.AttributeClass?.GetFullMetadataName() is PureAttributeName) ?? false)
            return ConstType.Pure;

        var attr = attrs?.FirstOrDefault(a => a.AttributeClass?.GetFullMetadataName() is ConstAttributeName);
        if (attr == null) return 0;
        var type = attr.NamedArguments.FirstOrDefault(p => p.Key == "Type").Value;
        var by = (ConstType?)type.Value ?? ConstType.AllConst;
        return by;
    }

    private static ConstType GetConstTypeAttribute(IMethodSymbol symbol)
        => GetConstTypeAttribute(symbol, s => s);

    private static ConstType GetConstTypeAttribute(IMethodSymbol symbol, int index)
        => GetConstTypeAttribute(symbol, s => s.Parameters[index]);

    private static ConstType GetConstTypeAttribute(IMethodSymbol symbol, Func<IMethodSymbol, ISymbol> getSymbol)
    {
        ConstType result = 0;

        var methodSymbol = symbol;
        do
        {
            result |= GetConstTypeAttributeRaw(getSymbol(methodSymbol));
            methodSymbol = methodSymbol.OverriddenMethod
                ?? GetInterfaceImplementation(methodSymbol);
        } while (methodSymbol is not null);

        return result;
    }
    public static IMethodSymbol? GetInterfaceImplementation(IMethodSymbol method)
    {
        var interfaceMethods =
            method.ContainingType.AllInterfaces.SelectMany(
                @interface => @interface.GetMembers().OfType<IMethodSymbol>());
        return interfaceMethods.FirstOrDefault(interfaceMethod => method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod)?.Equals(method, SymbolEqualityComparer.Default) ?? false);
    }
}