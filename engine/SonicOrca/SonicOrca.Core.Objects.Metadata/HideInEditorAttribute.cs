using System;

namespace SonicOrca.Core.Objects.Metadata;

/// <summary>
/// Prevents the editor from showing a public property or field as a state variable.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class HideInEditorAttribute : Attribute
{
}
