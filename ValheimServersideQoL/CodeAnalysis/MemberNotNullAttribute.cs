using System;
using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics.CodeAnalysis;

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
    public MemberNotNullAttribute(string member) => Members = [member];
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