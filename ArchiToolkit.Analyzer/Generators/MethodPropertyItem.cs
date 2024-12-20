﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchiToolkit.Analyzer.Generators;

public class MethodPropertyItem(
    PropertyDeclarationSyntax node,
    IPropertySymbol symbol,
    SemanticModel model,
    bool hasSet)
    : BasePropertyDependencyItem(node, symbol)
{
    public override IReadOnlyList<MemberDeclarationSyntax> GetMembers() =>
        [..base.GetMembers(), GetLazyField(), ..AccessMethods(), ..ClearModifyMethod(), InitMethod()];

    protected override AccessorDeclarationSyntax UpdateAccess(AccessorDeclarationSyntax accessor)
    {
        return accessor.Kind() switch
        {
            SyntaxKind.GetAccessorDeclaration => accessor.WithExpressionBody(
                ArrowExpressionClause(
                    BinaryExpression(
                        SyntaxKind.CoalesceExpression,
                        ConditionalAccessExpression(
                            IdentifierName(Name.LazyName),
                            MemberBindingExpression(
                                IdentifierName("Value"))),
                        LiteralExpression(
                            SyntaxKind.DefaultLiteralExpression,
                            Token(SyntaxKind.DefaultKeyword))))),
            SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration => accessor.WithExpressionBody(
                ArrowExpressionClause(InvocationExpression(IdentifierName(Name.SetName)).WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                IdentifierName("value"))))))),
            _ => accessor
        };
    }

    private FieldDeclarationSyntax GetLazyField()
    {
        return FieldDeclaration(
                VariableDeclaration(
                        GenericName(
                                Identifier("global::System.Lazy"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(TypeName)))))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(Name.LazyName)))))
            .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword)));
    }

    private IEnumerable<MethodDeclarationSyntax> AccessMethods()
    {
        var getMethod = MethodDeclaration(
                IdentifierName(TypeName),
                Identifier(Name.GetName))
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                IdentifierName("global::ArchiToolkit.Const"))))))
            .WithModifiers(
                TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.PartialKeyword)))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken));

        if (!hasSet) return [getMethod];

        var setMethod = SetOrModifyMethod(Name.SetName);

        return [getMethod, setMethod];
    }

    private MethodDeclarationSyntax SetOrModifyMethod(string methodName)
    {
        return MethodDeclaration(
                PredefinedType(
                    Token(SyntaxKind.VoidKeyword)),
                Identifier(methodName))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PartialKeyword)))
            .WithParameterList(
                ParameterList(
                    SingletonSeparatedList(
                        Parameter(
                                Identifier("value"))
                            .WithType(
                                IdentifierName(TypeName)))))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken));
    }

    private IEnumerable<MethodDeclarationSyntax> ClearModifyMethod()
    {
        IEnumerable<StatementSyntax> addition = [];
        List<MethodDeclarationSyntax> result = [];
        // if (Symbol.Type.IsReferenceType)
        // {
        //     result.Add(SetOrModifyMethod(Name.ModifyName));
        // }
        //
        // if (HasModifyMethodDeclaration())
        // {
        //     addition =
        //     [
        //         ExpressionStatement(
        //             InvocationExpression(
        //                     IdentifierName(Name.ModifyName))
        //                 .WithArgumentList(
        //                     ArgumentList(
        //                         SingletonSeparatedList(
        //                             Argument(
        //                                 MemberAccessExpression(
        //                                     SyntaxKind.SimpleMemberAccessExpression,
        //                                     IdentifierName(Name.LazyName),
        //                                     IdentifierName("Value"))))))),
        //         ExpressionStatement(
        //             ConditionalAccessExpression(
        //                 IdentifierName(Name.NameChanged),
        //                 InvocationExpression(
        //                     MemberBindingExpression(
        //                         IdentifierName("Invoke"))))),
        //         ReturnStatement()
        //     ];
        // }

        result.Add(MethodDeclaration(
                PredefinedType(
                    Token(SyntaxKind.VoidKeyword)),
                Identifier(Name.ClearName))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword)))
            .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
            .WithBody(
                Block(
                    IfStatement(
                        IsPatternExpression(
                            IdentifierName(Name.LazyName),
                            UnaryPattern(
                                ConstantPattern(
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression)))),
                        Block(
                            (StatementSyntax[])
                            [
                                IfStatement(
                                    PrefixUnaryExpression(
                                        SyntaxKind.LogicalNotExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(Name.LazyName),
                                            IdentifierName("IsValueCreated"))),
                                    ReturnStatement()),
                                ExpressionStatement(
                                    ConditionalAccessExpression(
                                        IdentifierName(Name.NameChanging),
                                        InvocationExpression(
                                            MemberBindingExpression(
                                                IdentifierName("Invoke"))))),
                                ..addition
                            ])),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(Name.LazyName),
                            ImplicitObjectCreationExpression()
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                IdentifierName(Name.GetName))))))),
                    ExpressionStatement(
                        ConditionalAccessExpression(
                            IdentifierName(Name.NameChanged),
                            InvocationExpression(
                                MemberBindingExpression(
                                    IdentifierName("Invoke"))))))));

        return result;
    }

    private MethodDeclarationSyntax InitMethod()
    {
        StatementSyntax[] items =
        [
            ExpressionStatement(
                InvocationExpression(
                    IdentifierName(Name.ClearName))),
            ..GetStatementsForInit()
        ];

        return MethodDeclaration(
                PredefinedType(
                    Token(SyntaxKind.VoidKeyword)),
                Identifier(Name.InitName))
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
            .WithAttributeLists(SingletonList(GeneratedCodeAttribute(typeof(PropertyDependencyGenerator))))
            .WithBody(Block(items));
    }

    private IEnumerable<StatementSyntax> GetStatementsForInit()
    {
        var accessors = GetAccessItems()
            .Where(i => !i.HasSymbol(Symbol) && i.ValidPropertySymbols.Any()).ToImmutableArray();

        return
        [
            ..accessors.Select(exp => exp.InvokeInit()),
            ReturnStatement(),
            ..accessors.Select(exp => exp.CreateInit(Name.ClearName)),
        ];
    }

    internal IEnumerable<PropertyAccessItem> GetAccessItems()
    {
        return GetExpressionBody().Select(exp => new PropertyAccessItem(exp, model))
            .ToImmutableHashSet(new PropertyAccessItemComparer());

        IReadOnlyList<ExpressionSyntax> GetExpressionBody()
        {
            var method = GetMethodDeclaration();
            if (method is null) return [];

            List<MethodDeclarationSyntax> methods = [method];
            IEnumerable<ExpressionSyntax> result = [];

            while (methods.Any())
            {
                List<MethodDeclarationSyntax> next = [];
                foreach (var m in methods)
                {
                    result = result.Concat(GetExpressions(m, out var invocations));

                    next.AddRange(invocations.Select(invocation => model.GetSymbolInfo(invocation).Symbol)
                        .OfType<ISymbol>()
                        .Where(s => s.ContainingSymbol.Equals(Symbol.ContainingSymbol, SymbolEqualityComparer.Default))
                        .Select(s => FindMethodDeclaration(s.Name))
                        .OfType<MethodDeclarationSyntax>());
                }

                methods = next;
            }

            return result.ToImmutableArray();
        }
    }

    private  IReadOnlyList<ExpressionSyntax> GetExpressions(MethodDeclarationSyntax method,
        out InvocationExpressionSyntax[] invocations)
    {
        var body = method.Body as SyntaxNode ?? method.ExpressionBody;
        if (body is null)
        {
            invocations = [];
            return [];
        }

        invocations = body.GetChildren<InvocationExpressionSyntax>().ToArray();
        return [..body.GetChildren<MemberAccessExpressionSyntax>()
            .Where(m => model.GetSymbolInfo(m).Symbol is IFieldSymbol or IPropertySymbol
                && m.Parent is not MemberAccessExpressionSyntax)
            .OfType<ExpressionSyntax>()
            .Concat(body.GetChildren<SimpleNameSyntax>()
                .Where(n =>
                {
                    var nameSymbol = model.GetSymbolInfo(n).Symbol;
                    return nameSymbol is IFieldSymbol or IPropertySymbol
                        && (n.Parent is not MemberAccessExpressionSyntax member || member.Name != n)
                        && nameSymbol.ContainingType.Equals(Symbol.ContainingType, SymbolEqualityComparer.Default);
                }))
            .SelectMany(GetMemberAccessFirst)];

        static IReadOnlyList<ExpressionSyntax> GetMemberAccessFirst(ExpressionSyntax expression)
        {
            return expression switch
            {
                ObjectCreationExpressionSyntax creation =>
                [
                    ..creation.ArgumentList?.Arguments.SelectMany(arg => GetMemberAccessFirst(arg.Expression)) ?? [],
                ],
                InvocationExpressionSyntax invocation =>
                [
                    invocation.Expression,
                    ..invocation.ArgumentList.Arguments.SelectMany(arg => GetMemberAccessFirst(arg.Expression))
                ],
                IdentifierNameSyntax name => [name],
                MemberAccessExpressionSyntax member => [member],
                AssignmentExpressionSyntax assignment => GetMemberAccessFirst(assignment.Right),
                BinaryExpressionSyntax binary =>
                [
                    ..GetMemberAccessFirst(binary.Left), ..GetMemberAccessFirst(binary.Right)
                ],
                _ => [],
            };
        }
    }

    // private bool HasModifyMethodDeclaration() => FindMethodDeclaration(Name.ModifyName) is not null;

    internal bool HasSetMethodDeclaration() => FindMethodDeclaration(Name.SetName) is not null;

    internal MethodDeclarationSyntax? GetMethodDeclaration() => FindMethodDeclaration(Name.GetName);

    private MethodDeclarationSyntax? FindMethodDeclaration(string name)
    {
        var type = Node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        return type?.GetChildren<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == name);
    }
}