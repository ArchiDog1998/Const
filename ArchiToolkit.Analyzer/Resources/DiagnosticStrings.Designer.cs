﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ArchiToolkit.Analyzer.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class DiagnosticStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal DiagnosticStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ArchiToolkit.Analyzer.Resources.DiagnosticStrings", typeof(DiagnosticStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You can only add the accessor get or set.
        /// </summary>
        internal static string AccessorTypePropertyDescriptorMessage {
            get {
                return ResourceManager.GetString("AccessorTypePropertyDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You can only add the accessor get or set.
        /// </summary>
        internal static string AccessorTypePropertyDescriptorTittle {
            get {
                return ResourceManager.GetString("AccessorTypePropertyDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t add body to the property.
        /// </summary>
        internal static string BodyPropertyDecriptorTittle {
            get {
                return ResourceManager.GetString("BodyPropertyDecriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t add body to this property .
        /// </summary>
        internal static string BodyPropertyDescriptorMessage {
            get {
                return ResourceManager.GetString("BodyPropertyDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to How to access this identifier name in Expression?.
        /// </summary>
        internal static string CantFindDescriptorMessage {
            get {
                return ResourceManager.GetString("CantFindDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Where is its name?.
        /// </summary>
        internal static string CantFindDescriptorTittle {
            get {
                return ResourceManager.GetString("CantFindDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You cannot modify member &apos;{0}&apos;, because you have set this method to Const.{1}..
        /// </summary>
        internal static string MemberDescriptorMessage {
            get {
                return ResourceManager.GetString("MemberDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t modify this member.
        /// </summary>
        internal static string MemberDescriptorTittle {
            get {
                return ResourceManager.GetString("MemberDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You cannot invoke method &apos;{2}&apos;, because you have set this method to Const.{1}. You can try to modify the Const Attribute in &apos;{2}&apos;..
        /// </summary>
        internal static string MemberInvokeDescriptorMessage {
            get {
                return ResourceManager.GetString("MemberInvokeDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t invoke this member&apos;s method.
        /// </summary>
        internal static string MemberInvokeDescriptorTittle {
            get {
                return ResourceManager.GetString("MemberInvokeDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You cannot invoke method &apos;{0}&apos;, because you have set this method to Const.{1}..
        /// </summary>
        internal static string MethodDescriptorMessage {
            get {
                return ResourceManager.GetString("MethodDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t invoke this method.
        /// </summary>
        internal static string MethodDescriptorTittle {
            get {
                return ResourceManager.GetString("MethodDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You cannot modify parameter &apos;{0}&apos;, because you have set it to Const.{1}..
        /// </summary>
        internal static string ParameterDescriptorMessage {
            get {
                return ResourceManager.GetString("ParameterDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t modify this parameter.
        /// </summary>
        internal static string ParameterDescriptorTittle {
            get {
                return ResourceManager.GetString("ParameterDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You cannot invoke method &apos;{2}&apos;, because you have set the parameter &apos;{0}&apos; to Const.{1}. You can try to modify the Const Attribute in &apos;{2}&apos;..
        /// </summary>
        internal static string ParameterInvokeDescriptorMessage {
            get {
                return ResourceManager.GetString("ParameterInvokeDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t invoke this parameter&apos;s method.
        /// </summary>
        internal static string ParameterInvokeDescriptorTittle {
            get {
                return ResourceManager.GetString("ParameterInvokeDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t call this property in this method.
        /// </summary>
        internal static string PartialMethodCallSelfDescriptorMessage {
            get {
                return ResourceManager.GetString("PartialMethodCallSelfDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t call this property in this method.
        /// </summary>
        internal static string PartialMethodCallSelfDescriptorTittle {
            get {
                return ResourceManager.GetString("PartialMethodCallSelfDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please add partial method for getting the property &apos;{0}&apos;.
        /// </summary>
        internal static string PartialMethodDescriptorMessage {
            get {
                return ResourceManager.GetString("PartialMethodDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please add partial method for getting this property.
        /// </summary>
        internal static string PartialMethodDescriptorTittle {
            get {
                return ResourceManager.GetString("PartialMethodDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please add the &apos;partial&apos; keyword to the property &apos;{0}&apos;.
        /// </summary>
        internal static string PartialPropertyDecriptorMesage {
            get {
                return ResourceManager.GetString("PartialPropertyDecriptorMesage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add &apos;partial&apos; keyword to the property.
        /// </summary>
        internal static string PartialPropertyDescriptorTittle {
            get {
                return ResourceManager.GetString("PartialPropertyDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please add partial method for setting the property &apos;{0}&apos;.
        /// </summary>
        internal static string PartialSetMethodDescriptorMessage {
            get {
                return ResourceManager.GetString("PartialSetMethodDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please add partial method for setting this property.
        /// </summary>
        internal static string PartialSetMethodDescriptorTittle {
            get {
                return ResourceManager.GetString("PartialSetMethodDescriptorTittle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t add the static to this property when you add the attribute Prop..
        /// </summary>
        internal static string PartialStaticDescriptorMessage {
            get {
                return ResourceManager.GetString("PartialStaticDescriptorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t add the static to this property when you add the attribute Prop..
        /// </summary>
        internal static string PartialStaticDescriptorTittle {
            get {
                return ResourceManager.GetString("PartialStaticDescriptorTittle", resourceCulture);
            }
        }
    }
}
