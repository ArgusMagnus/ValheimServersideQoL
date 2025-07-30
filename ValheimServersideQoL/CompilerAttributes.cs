﻿using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IsExternalInit;

    //
    // Summary:
    //     Indicates that compiler support for a particular feature is required for the
    //     location where this attribute is applied.
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        //
        // Summary:
        //     The System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute.FeatureName
        //     used for the ref structs C# feature.
        public const string RefStructs = "RefStructs";
        //
        // Summary:
        //     The System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute.FeatureName
        //     used for the required members C# feature.
        public const string RequiredMembers = "RequiredMembers";

        //
        // Summary:
        //     Initializes a System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute
        //     instance for the passed in compiler feature.
        //
        // Parameters:
        //   featureName:
        //     The name of the required compiler feature.
        public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;

        //
        // Summary:
        //     The name of the compiler feature.
        public string FeatureName { get; }
        //
        // Summary:
        //     Gets a value that indicates whether the compiler can choose to allow access to
        //     the location where this attribute is applied if it does not understand System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute.FeatureName.
        //
        //
        // Returns:
        //     true to let the compiler choose to allow access to the location where this attribute
        //     is applied if it does not understand System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute.FeatureName;
        //     otherwise, false.
        public bool IsOptional { get; init; }
    }

    //
    // Summary:
    //     Specifies that a type has required members or that a member is required.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    sealed class RequiredMemberAttribute : Attribute;
}

namespace System.Diagnostics.CodeAnalysis
{
    //
    // Summary:
    //     Specifies that this constructor sets all required members for the current type,
    //     and callers do not need to set any required members themselves.
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    sealed class SetsRequiredMembersAttribute : Attribute;
}