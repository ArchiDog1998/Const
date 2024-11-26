using System.Collections.Immutable;
using ArchiToolkit.Analyzer.Analyzers;
using ArchiToolkit.Analyzer.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ArchiToolkit.Analyzer;

public static class DiagnosticExtensions
{
    public const string PartialPropertyDiagnosticId = "AC1101", PartialMethodDiagnosticId = "AC1104", PartialSetMethodDiagnosticId = "AC1107";
    
    public static ImmutableArray<DiagnosticDescriptor> BaseDpDescriptors =>
    [
        PartialPropertyDescriptor,
        PartialStaticDescriptor,
        BodyPropertyDescriptor,
        AccessorTypePropertyDescriptor,
#if DEBUG
        DebugMessageDescriptor,
#endif
    ];
    
    public static ImmutableArray<DiagnosticDescriptor> PropDpDescriptors =>
    [
        PartialMethodDescriptor,
        PartialSetMethodDescriptor,
        PartialMethodCallSelfDescriptor,
    ];

    public static ImmutableArray<DiagnosticDescriptor> FieldDpDescriptors =>
    [
        AttributeTypeDescriptor,
    ];

    public static ImmutableArray<DiagnosticDescriptor> ConstDescriptors =>
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
    
    private static DiagnosticDescriptor CreateUsageErrorDescriptor(string id, string title, string messageFormat, DiagnosticSeverity severity = DiagnosticSeverity.Error) 
        => new(id, Local(title), Local(messageFormat), "Usage", severity, true);
    
    private static readonly DiagnosticDescriptor PartialPropertyDescriptor = CreateUsageErrorDescriptor(PartialPropertyDiagnosticId, 
        nameof(DiagnosticStrings.PartialPropertyDescriptorTittle), nameof(DiagnosticStrings.PartialPropertyDecriptorMesage));
    
    private static readonly DiagnosticDescriptor BodyPropertyDescriptor = CreateUsageErrorDescriptor("AC1102", 
        nameof(DiagnosticStrings.BodyPropertyDecriptorTittle), nameof(DiagnosticStrings.BodyPropertyDescriptorMessage));
    
    private static readonly DiagnosticDescriptor AccessorTypePropertyDescriptor = CreateUsageErrorDescriptor("AC1103", 
        nameof(DiagnosticStrings.AccessorTypePropertyDescriptorTittle), nameof(DiagnosticStrings.AccessorTypePropertyDescriptorMessage));
    
    private static readonly DiagnosticDescriptor PartialMethodDescriptor = CreateUsageErrorDescriptor(PartialMethodDiagnosticId, 
        nameof(DiagnosticStrings.PartialMethodDescriptorTittle), nameof(DiagnosticStrings.PartialMethodDescriptorMessage));

    private static readonly DiagnosticDescriptor PartialMethodCallSelfDescriptor = CreateUsageErrorDescriptor("AC1105", 
        nameof(DiagnosticStrings.PartialMethodCallSelfDescriptorTittle), nameof(DiagnosticStrings.PartialMethodCallSelfDescriptorMessage));
    
    private static readonly DiagnosticDescriptor PartialStaticDescriptor = CreateUsageErrorDescriptor("AC1106", 
        nameof(DiagnosticStrings.PartialStaticDescriptorTittle), nameof(DiagnosticStrings.PartialStaticDescriptorMessage));
    
    private static readonly DiagnosticDescriptor PartialSetMethodDescriptor = CreateUsageErrorDescriptor(PartialSetMethodDiagnosticId, 
        nameof(DiagnosticStrings.PartialSetMethodDescriptorTittle), nameof(DiagnosticStrings.PartialSetMethodDescriptorMessage));
    
    private static readonly DiagnosticDescriptor AttributeTypeDescriptor = CreateUsageErrorDescriptor("AC1108", 
        nameof(DiagnosticStrings.AttributeTypeDescriptorTittle), nameof(DiagnosticStrings.AttributeTypeDescriptorMessage), DiagnosticSeverity.Warning);
    
    private static readonly DiagnosticDescriptor ParameterDescriptor = CreateUsageErrorDescriptor("AC1001",
        nameof(DiagnosticStrings.ParameterDescriptorTittle), nameof(DiagnosticStrings.ParameterDescriptorMessage));
    
    private static readonly DiagnosticDescriptor MemberDescriptor = CreateUsageErrorDescriptor("AC1002",
        nameof(DiagnosticStrings.MemberDescriptorTittle), nameof(DiagnosticStrings.MemberDescriptorMessage));
    
    private static readonly DiagnosticDescriptor MethodDescriptor = CreateUsageErrorDescriptor("AC1003",
        nameof(DiagnosticStrings.MethodDescriptorTittle), nameof(DiagnosticStrings.MethodDescriptorMessage));
    
    private static readonly DiagnosticDescriptor ParameterInvokeDescriptor = CreateUsageErrorDescriptor("AC1004",
        nameof(DiagnosticStrings.ParameterInvokeDescriptorTittle), nameof(DiagnosticStrings.ParameterInvokeDescriptorMessage));
    
    private static readonly DiagnosticDescriptor MemberInvokeDescriptor = CreateUsageErrorDescriptor("AC1005",
        nameof(DiagnosticStrings.MemberInvokeDescriptorTittle), nameof(DiagnosticStrings.MemberInvokeDescriptorMessage));

    public static void ReportAttributeType(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
        => ReportDescriptor(context, AttributeTypeDescriptor, syntaxNode);
    
    public static void ReportPartialStatic(this SyntaxNodeAnalysisContext context, SyntaxToken syntaxNode)
        => ReportDescriptor(context, PartialStaticDescriptor, syntaxNode);
    
    public static void ReportAccessorType(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
        => ReportDescriptor(context, AccessorTypePropertyDescriptor, syntaxNode);
    
    public static void ReportPartialMethod(this SyntaxNodeAnalysisContext context, SyntaxToken syntaxNode)
        => ReportDescriptor(context, PartialMethodDescriptor, syntaxNode);
    
    public static void ReportPartialSetMethod(this SyntaxNodeAnalysisContext context, SyntaxToken syntaxNode)
        => ReportDescriptor(context, PartialSetMethodDescriptor, syntaxNode);
    
    public static void ReportPartialMethodCallSelf(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
        => ReportDescriptor(context, PartialMethodCallSelfDescriptor, syntaxNode);
    
    public static void ReportBody(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
        => ReportDescriptor(context, BodyPropertyDescriptor, syntaxNode);
    
    public static void ReportPartial(this SyntaxNodeAnalysisContext context, SyntaxToken syntaxNode)
        => ReportDescriptor(context, PartialPropertyDescriptor, syntaxNode);
    
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

    private static DiagnosticDescriptor CreateToolWarningDescriptor(string id, string title, string messageFormat) 
        => new(id, Local(title), Local(messageFormat), "Tool", DiagnosticSeverity.Warning, true);
    
    private static readonly DiagnosticDescriptor CantFindDescriptor = CreateToolWarningDescriptor("AC2001",
        nameof(DiagnosticStrings.CantFindDescriptorTittle), nameof(DiagnosticStrings.CantFindDescriptorMessage));
    
    public static void ReportCantFind(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
        => ReportDescriptor(context, CantFindDescriptor, syntaxNode);

#if DEBUG
    private static readonly DiagnosticDescriptor DebugMessageDescriptor =  
#pragma warning disable RS2000
        new("AC2000", "Debug Message", "{1}", "Tool", DiagnosticSeverity.Warning, true);
#pragma warning restore RS2000
    
    public static void ReportDebugMessage(this SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode, string message)
        => ReportDescriptor(context, DebugMessageDescriptor, syntaxNode, message);
#endif
    #endregion

    private static void ReportDescriptor(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params object?[] args)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, syntaxNode.GetLocation(), [syntaxNode, ..args]));
    
    private static void ReportDescriptor(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken syntaxNode, params object?[] args)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, syntaxNode.GetLocation(), [syntaxNode, ..args]));
}