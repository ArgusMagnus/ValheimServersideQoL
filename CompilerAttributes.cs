using System.ComponentModel;

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
    //     Specifies that when a method returns System.Diagnostics.CodeAnalysis.NotNullWhenAttribute.ReturnValue,
    //     the parameter will not be null even if the corresponding type allows it.
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    sealed class NotNullWhenAttribute : Attribute
    {
        //
        // Summary:
        //     Initializes the attribute with the specified return value condition.
        //
        // Parameters:
        //   returnValue:
        //     The return value condition. If the method returns this value, the associated
        //     parameter will not be null.
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        //
        // Summary:
        //     Gets the return value condition.
        //
        // Returns:
        //     The return value condition. If the method returns this value, the associated
        //     parameter will not be null.
        public bool ReturnValue { get; }
    }

    //
    // Summary:
    //     Specifies that when a method returns System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute.ReturnValue,
    //     the parameter may be null even if the corresponding type disallows it.
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    sealed class MaybeNullWhenAttribute : Attribute
    {
        //
        // Summary:
        //     Initializes the attribute with the specified return value condition.
        //
        // Parameters:
        //   returnValue:
        //     The return value condition. If the method returns this value, the associated
        //     parameter may be null.
        public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        //
        // Summary:
        //     Gets the return value condition.
        //
        // Returns:
        //     The return value condition. If the method returns this value, the associated
        //     parameter may be null.
        public bool ReturnValue { get; }
    }

    //
    // Summary:
    //     Specifies that the method or property will ensure that the listed field and property
    //     members have values that aren't null.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class MemberNotNullAttribute : Attribute
    {
        //
        // Summary:
        //     Initializes the attribute with a field or property member.
        //
        // Parameters:
        //   member:
        //     The field or property member that is promised to be non-null.
        public MemberNotNullAttribute(string member) : this(new[] { member }) { }
        //
        // Summary:
        //     Initializes the attribute with the list of field and property members.
        //
        // Parameters:
        //   members:
        //     The list of field and property members that are promised to be non-null.
        public MemberNotNullAttribute(params string[] members) => Members = members;

        //
        // Summary:
        //     Gets field or property member names.
        public string[] Members { get; }
    }

    //
    // Summary:
    //     Specifies that the method or property will ensure that the listed field and property
    //     members have non-null values when returning with the specified return value condition.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class MemberNotNullWhenAttribute : Attribute
    {
        //
        // Summary:
        //     Initializes the attribute with the specified return value condition and a field
        //     or property member.
        //
        // Parameters:
        //   returnValue:
        //     The return value condition. If the method returns this value, the associated
        //     parameter will not be null.
        //
        //   member:
        //     The field or property member that is promised to be non-null.
        public MemberNotNullWhenAttribute(bool returnValue, string member)
            : this(returnValue, new[] { member }) { }
        //
        // Summary:
        //     Initializes the attribute with the specified return value condition and list
        //     of field and property members.
        //
        // Parameters:
        //   returnValue:
        //     The return value condition. If the method returns this value, the associated
        //     parameter will not be null.
        //
        //   members:
        //     The list of field and property members that are promised to be non-null.
        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }

        //
        // Summary:
        //     Gets field or property member names.
        public string[] Members { get; }
        //
        // Summary:
        //     Gets the return value condition.
        public bool ReturnValue { get; }
    }

    //
    // Summary:
    //     Specifies that this constructor sets all required members for the current type,
    //     and callers do not need to set any required members themselves.
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    sealed class SetsRequiredMembersAttribute : Attribute;
}