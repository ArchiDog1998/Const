; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
AC1001  | Usage    | Error    | Don't modify this parameter          
AC1002  | Usage    | Error    | Don't modify this member             
AC1003  | Usage    | Error    | Don't invoke this method             
AC1004  | Usage    | Error    | Don't invoke this parameter's method 
AC1005  | Usage    | Error    | Don't invoke this member's method    
AC1101 | Usage | Error    | Add 'partial' keyword to the property
AC1102 | Usage | Error | Don't add body to the property
AC1103 | Usage | Error | You can only add the accessor get or set
AC1104 | Usage | Error | Please add partial method for getting this property
AC1105 | Usage | Error | DiagnosticExtensions
AC2001  | Tool     | Warning  | Where is its name?                   
