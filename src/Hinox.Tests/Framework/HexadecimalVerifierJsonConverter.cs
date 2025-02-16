namespace SceneGate.Hinox.Tests.Framework;

using System;
using System.Linq;
using System.Reflection;
using VerifyTests;

/// <summary>
/// JSON converter of objects with integer values in hexadecimal.
/// </summary>
/// <typeparam name="T"></typeparam>
public class HexadecimalVerifierJsonConverter<T> : WriteOnlyJsonConverter<T>
{
    private static readonly Type[] HexadecimalTypes = [
        typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
    ];

    /// <inheritdoc />
    public override void Write(VerifyJsonWriter writer, T value)
    {
        writer.WriteStartObject();

        PropertyInfo[] objProperties = value.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);

        foreach (PropertyInfo prop in objProperties) {
            if (HexadecimalTypes.Contains(prop.PropertyType)) {
                writer.WriteMember(value, $"0x{prop.GetValue(value):X}", prop.Name);
            } else {
                writer.WriteMember(value, prop.GetValue(value), prop.Name);
            }
        }

        writer.WriteEndObject();
    }
}
