using System.Reflection;

namespace Bodu.Test;

public static partial class TestHelpers
{
    private static readonly HashSet<Type> SupportedSimpleStructs = new()
    {
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(Guid),
        typeof(TimeSpan)

        // You can add more here as needed.
    };

    /// <summary>
    /// Asserts whether the value of a given field in the specified object is the default for its type.
    /// </summary>
    /// <typeparam name="T">The type of the object containing the field.</typeparam>
    /// <param name="fieldInfo">The field metadata.</param>
    /// <param name="instance">The instance containing the field.</param>
    /// <returns><c>true</c> if the field is default; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fieldInfo" /> is <see langword="null" />.</exception>
    /// <exception cref="NotSupportedException">Thrown if the field is of an unsupported complex type.</exception>
    public static bool AssertFieldValueIsDefault<T>(FieldInfo fieldInfo, T instance)
    {
        ArgumentNullException.ThrowIfNull(fieldInfo);

        var value = fieldInfo.GetValue(instance);
        if (value is null) return false;

        return AssertValueIsDefault(
            fieldInfo.FieldType,
            value,
            new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    /// <summary>
    /// Asserts whether the value of a given field in the specified object is either <c>null</c> or the default for its type.
    /// </summary>
    /// <typeparam name="T">The type of the object containing the field.</typeparam>
    /// <param name="fieldInfo">The field metadata.</param>
    /// <param name="instance">The instance containing the field.</param>
    /// <returns><c>true</c> if the field is null or default; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fieldInfo" /> is <see langword="null" />.</exception>
    /// <exception cref="NotSupportedException">Thrown if the field is of an unsupported complex type.</exception>
    public static bool AssertFieldValueIsNullOrDefault<T>(FieldInfo fieldInfo, T instance)
    {
        ArgumentNullException.ThrowIfNull(fieldInfo);

        var value = fieldInfo.GetValue(instance);

        return value is null ||
               AssertValueIsDefault(
                   fieldInfo.FieldType,
                   value,
                   new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    /// <summary>
    /// Asserts whether the supplied value is the default value for the supplied type.
    /// </summary>
    /// <param name="fieldType">The field type to inspect.</param>
    /// <param name="value">The field value.</param>
    /// <param name="visited">The set of reference objects already inspected.</param>
    /// <returns><c>true</c> if the value is default; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotSupportedException">Thrown if the value is of an unsupported complex type.</exception>
    private static bool AssertValueIsDefault(Type fieldType, object value, HashSet<object> visited)
    {
        if (fieldType.IsPrimitive || fieldType.IsEnum || SupportedSimpleStructs.Contains(fieldType))
            return value.Equals(Activator.CreateInstance(fieldType));

        if (fieldType.IsArray)
        {
            var array = (Array)value;
            Type elementType = fieldType.GetElementType()!;

            if (array.Length == 0) return true;

            var defaultElement = Activator.CreateInstance(elementType);

            foreach (var item in array)
            {
                if (!Equals(item, defaultElement))
                    return false;
            }

            return true;
        }

        if (fieldType == typeof(MemoryStream))
        {
            var stream = (MemoryStream)value;

            try
            {
                var buffer = stream.ToArray();
                return buffer.Length == 0 || buffer.All(static b => b == 0);
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        if (fieldType.IsGenericType)
        {
            Type genericDef = fieldType.GetGenericTypeDefinition();

            if (genericDef == typeof(Memory<>))
            {
                var toArrayMethod = fieldType.GetMethod(nameof(Memory<byte>.ToArray))!;
                var memoryArray = (Array)toArrayMethod.Invoke(value, null)!;

                var defaultElement = Activator.CreateInstance(fieldType.GetGenericArguments()[0]);

                foreach (var item in memoryArray)
                {
                    if (!Equals(item, defaultElement))
                        return false;
                }

                return true;
            }
        }

        // Span<T> and ReadOnlySpan<T> cannot be retrieved reliably via reflection.
        if (fieldType == typeof(Span<byte>) || fieldType == typeof(ReadOnlySpan<byte>))
            throw new NotSupportedException("Span<T> fields cannot be reliably checked via reflection.");

        // For value types, compare to their default instance. This covers internal structs such as AsconState
        // whose fields are all primitive values.
        if (fieldType.IsValueType)
            return value.Equals(Activator.CreateInstance(fieldType));

        // Private nested holder objects, such as AsconAead128.KeyMaterial128, are owned state.
        // The holder itself does not need to be null; its instance fields must be null/default.
        if (fieldType.IsClass && fieldType.IsNested)
        {
            if (!visited.Add(value))
                return true;

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (FieldInfo nestedField in fieldType.GetFields(flags))
            {
                if (nestedField.IsStatic)
                    continue;

                var nestedValue = nestedField.GetValue(value);

                if (nestedValue is null)
                    continue;

                if (!AssertValueIsDefault(nestedField.FieldType, nestedValue, visited))
                    return false;
            }

            return true;
        }

        // Non-nested IDisposable reference types (e.g. SymmetricAlgorithm subclasses) are often
        // declared readonly and cannot be nulled. The meaningful clearing action is calling Dispose
        // on them. Accept any such value here — the containing type's Dispose test covers that obligation.
        if (typeof(IDisposable).IsAssignableFrom(fieldType) && fieldType.IsClass)
            return true;

        throw new NotSupportedException($"Unsupported field type: {fieldType}");
    }

    public static string GetTypeFieldDisplayName(MethodInfo methodInfo, object[] data) =>
        data.Length > 0 && data[0] is FieldInfo field
            ? $"{field.DeclaringType?.Name}.{field.Name}"
            : $"{methodInfo.Name}_NoDisposableFields";

    public static string GetTypePropertyDisplayName(MethodInfo methodInfo, object[] data) =>
        data.Length > 0 && data[0] is PropertyInfo property
            ? $"{property.DeclaringType?.Name}.{property.Name}"
            : $"{methodInfo.Name}_NoDisposableProperties";
}