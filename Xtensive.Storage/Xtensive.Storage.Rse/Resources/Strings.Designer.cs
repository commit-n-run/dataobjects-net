﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Xtensive.Storage.Rse.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Xtensive.Storage.Rse.Resources.Strings", typeof(Strings).Assembly);
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
        ///   Looks up a localized string similar to At least one column index pair must be specified..
        /// </summary>
        internal static string ExAtLeastOneColumnIndexPairMustBeSpecified {
            get {
                return ResourceManager.GetString("ExAtLeastOneColumnIndexPairMustBeSpecified", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Can&apos;t compile the provider &apos;{0}&apos;..
        /// </summary>
        internal static string ExCantCompileProviderX {
            get {
                return ResourceManager.GetString("ExCantCompileProviderX", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not resolve {0} &apos;{1}&apos; within the domain..
        /// </summary>
        internal static string ExCouldNotResolveXYWithinDomain {
            get {
                return ResourceManager.GetString("ExCouldNotResolveXYWithinDomain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid field name &apos;{0}&apos;..
        /// </summary>
        internal static string ExInvalidFieldNameX {
            get {
                return ResourceManager.GetString("ExInvalidFieldNameX", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider must be either CompilableProvider or ExecutableProvider.
        /// </summary>
        internal static string ExProviderMustBeEitherCompilableProviderOrExecutableProvider {
            get {
                return ResourceManager.GetString("ExProviderMustBeEitherCompilableProviderOrExecutableProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; must be active..
        /// </summary>
        internal static string ExXMustBeActive {
            get {
                return ResourceManager.GetString("ExXMustBeActive", resourceCulture);
            }
        }
    }
}
