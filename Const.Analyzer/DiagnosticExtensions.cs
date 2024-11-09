using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Const.Analyzer;

public static class DiagnosticExtensions
{
    public static ImmutableArray<DiagnosticDescriptor> Descriptors =>
    [
        ParameterDescriptor, MemberDescriptor, MethodDescriptor,
        ParameterInvokeDescriptor, MemberInvokeDescriptor,
        CantFindDescriptor,
#if DEBUG
        DebugMessageDescriptor,
#endif
    ];

    private static LocalizableResourceString Local(string nameOfLocalizableString) =>
        new(nameOfLocalizableString, DiagnosticStrings.ResourceManager, typeof(DiagnosticStrings));

    #region Usage Diagnotic

    private static DiagnosticDescriptor CreateUsageErrorDescriptor(int id, string title, string messageFormat) 
        => new($"CT1{id:D3}", Local(title), Local(messageFormat), "Usage", DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor ParameterDescriptor = CreateUsageErrorDescriptor(1,
        nameof(DiagnosticStrings.ParameterDescriptorTittle), nameof(DiagnosticStrings.ParameterDescriptorMessage));
    
    private static readonly DiagnosticDescriptor MemberDescriptor = CreateUsageErrorDescriptor(2,
        nameof(DiagnosticStrings.MemberDescriptorTittle), nameof(DiagnosticStrings.MemberDescriptorMessage));
    
    private static readonly DiagnosticDescriptor MethodDescriptor = CreateUsageErrorDescriptor(3,
        nameof(DiagnosticStrings.MethodDescriptorTittle), nameof(DiagnosticStrings.MethodDescriptorMessage));
    
    private static readonly DiagnosticDescriptor ParameterInvokeDescriptor = CreateUsageErrorDescriptor(4,
        nameof(DiagnosticStrings.ParameterInvokeDescriptorTittle), nameof(DiagnosticStrings.ParameterInvokeDescriptorMessage));
    
    private static readonly DiagnosticDescriptor MemberInvokeDescriptor = CreateUsageErrorDescriptor(5,
        nameof(DiagnosticStrings.MemberInvokeDescriptorTittle), nameof(DiagnosticStrings.MemberInvokeDescriptorMessage));
    
    public static void ReportParameter(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode, ConstType type)
        => ReportDescriptor(context, ParameterDescriptor, syntaxNode, type);
    
    public static void ReportMember(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode, ConstType type)
        => ReportDescriptor(context, MemberDescriptor, syntaxNode, type);
        
    public static void ReportMethod(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode, ConstType type)
        => ReportDescriptor(context, MethodDescriptor, syntaxNode, type);
    
    public static void ReportParameterInvoke(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode, ConstType type, string methodName)
        => ReportDescriptor(context, ParameterInvokeDescriptor, syntaxNode, type, methodName);
    
    public static void ReportMemberInvoke(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode, ConstType type, string methodName)
        => ReportDescriptor(context, MemberInvokeDescriptor, syntaxNode, type, methodName);

    #endregion

    #region ToolBug Diagnotic

    private static DiagnosticDescriptor CreateToolWarningDescriptor(int id, string title, string messageFormat) 
        => new($"CT2{id:D3}", Local(title), Local(messageFormat), "Tool", DiagnosticSeverity.Warning, true);
    
    private static readonly DiagnosticDescriptor CantFindDescriptor = CreateToolWarningDescriptor(1,
        nameof(DiagnosticStrings.CantFindDescriptorTittle), nameof(DiagnosticStrings.CantFindDescriptorMessage));
    
    public static void ReportCantFind(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
        => ReportDescriptor(context, CantFindDescriptor, syntaxNode);

#if DEBUG
    private static readonly DiagnosticDescriptor DebugMessageDescriptor =  
#pragma warning disable RS2000
        new("CT2000", "Debug Message", "{1}", "Tool", DiagnosticSeverity.Warning, true);
#pragma warning restore RS2000
    
    public static void ReportDebugMessage(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode, string message)
        => ReportDescriptor(context, DebugMessageDescriptor, syntaxNode, message);
#endif
    #endregion

    private static void ReportDescriptor(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params object?[] args)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, syntaxNode.GetLocation(), [syntaxNode, ..args]));
}