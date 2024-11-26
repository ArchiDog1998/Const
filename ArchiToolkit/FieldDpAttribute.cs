namespace ArchiToolkit;

/// <summary>
/// Add this property to the property you want to make as data provider.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class FieldDpAttribute : Attribute
{
    /// <summary>
    /// The <see cref="IEqualityComparer{T}"/> of this field, please notify the generic type.
    /// </summary>
    public Type? Comparer { get; set; }
}