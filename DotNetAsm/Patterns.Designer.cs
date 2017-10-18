﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DotNetAsm {
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
    internal class Patterns {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Patterns() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DotNetAsm.Patterns", typeof(Patterns).Assembly);
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
        ///   Looks up a localized string similar to [a-zA-Z][a-zA-Z0-9]*.
        /// </summary>
        internal static string SymbolBasic {
            get {
                return ResourceManager.GetString("SymbolBasic", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [a-zA-Z0-9_.]*.
        /// </summary>
        internal static string SymbolChar {
            get {
                return ResourceManager.GetString("SymbolChar", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to _?[a-zA-Z][a-zA-Z0-9_.]*.
        /// </summary>
        internal static string SymbolDot {
            get {
                return ResourceManager.GetString("SymbolDot", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ^_?[a-zA-Z][a-zA-Z0-9_]*$.
        /// </summary>
        internal static string SymbolFull {
            get {
                return ResourceManager.GetString("SymbolFull", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [a-zA-Z][a-zA-Z0-9_]*.
        /// </summary>
        internal static string SymbolUnderscore {
            get {
                return ResourceManager.GetString("SymbolUnderscore", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [\p{Ll}\p{Lu}\p{Lt}][\p{Ll}\p{Lu}\p{Lt}0-9_]*.
        /// </summary>
        internal static string SymbolUnicode {
            get {
                return ResourceManager.GetString("SymbolUnicode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [\p{Ll}\p{Lu}\p{Lt}0-9_.].
        /// </summary>
        internal static string SymbolUnicodeChar {
            get {
                return ResourceManager.GetString("SymbolUnicodeChar", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to _?[\p{Ll}\p{Lu}\p{Lt}][\p{Ll}\p{Lu}\p{Lt}0-9_.]*.
        /// </summary>
        internal static string SymbolUnicodeDot {
            get {
                return ResourceManager.GetString("SymbolUnicodeDot", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ^_?[\p{Ll}\p{Lu}\p{Lt}][\p{Ll}\p{Lu}\p{Lt}0-9_.]*$.
        /// </summary>
        internal static string SymbolUnicodeFull {
            get {
                return ResourceManager.GetString("SymbolUnicodeFull", resourceCulture);
            }
        }
    }
}
