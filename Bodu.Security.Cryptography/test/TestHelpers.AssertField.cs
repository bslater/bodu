using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bodu
{
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
        /// <exception cref="NotSupportedException">Thrown if the field is of an unsupported complex type.</exception>
        public static bool AssertFieldValueIsDefault<T>(FieldInfo fieldInfo, T instance)
        {
            object? value = fieldInfo.GetValue(instance);
            if (value == null) return false;

            Type fieldType = fieldInfo.FieldType;

            if (fieldType.IsPrimitive || fieldType.IsEnum || SupportedSimpleStructs.Contains(fieldType))
                return value.Equals(Activator.CreateInstance(fieldType));

            if (fieldType.IsArray)
            {
                var array = (Array)value;
                var elementType = fieldType.GetElementType()!;
                if (array.Length == 0) return true;

                object? defaultElement = Activator.CreateInstance(elementType);
                foreach (var item in array)
                {
                    if (!Equals(item, defaultElement))
                        return false;
                }
                return true;
            }

            if (fieldType.IsGenericType)
            {
                var genericDef = fieldType.GetGenericTypeDefinition();
                if (genericDef == typeof(Memory<>))
                {
                    var toArrayMethod = fieldType.GetMethod("ToArray")!;
                    var memoryArray = (Array)toArrayMethod.Invoke(value, null)!;

                    object? defaultElement = Activator.CreateInstance(fieldType.GetGenericArguments()[0]);
                    foreach (var item in memoryArray)
                    {
                        if (!Equals(item, defaultElement))
                            return false;
                    }
                    return true;
                }
            }

            // Span<T> and ReadOnlySpan<T> cannot be retrieved via reflection
            if (fieldType == typeof(Span<byte>) || fieldType == typeof(ReadOnlySpan<byte>))
                throw new NotSupportedException("Span<TAlgorithm> fields cannot be reliably checked via reflection.");

            throw new NotSupportedException($"Unsupported field type: {fieldType}");
        }

        /// <summary>
        /// Asserts whether the value of a given field in the specified object is either <c>null</c> or the default for its type.
        /// </summary>
        /// <typeparam name="T">The type of the object containing the field.</typeparam>
        /// <param name="fieldInfo">The field metadata.</param>
        /// <param name="instance">The instance containing the field.</param>
        /// <returns><c>true</c> if the field is null or default; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">Thrown if the field is of an unsupported complex type.</exception>
        public static bool AssertFieldValueIsNullOrDefault<T>(FieldInfo fieldInfo, T instance) =>
            fieldInfo.GetValue(instance) is null || AssertFieldValueIsDefault(fieldInfo, instance);
    }
}