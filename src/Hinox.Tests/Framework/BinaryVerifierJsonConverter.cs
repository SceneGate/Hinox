namespace SceneGate.Hinox.Tests.Framework;

using System;
using System.IO;
using System.Security.Cryptography;
using VerifyTests;
using Yarhl.IO;

/// <summary>
/// JSON converter for Yarhl binary formats implementing <see cref="IBinary"/>.
/// </summary>
public class BinaryVerifierJsonConverter : WriteOnlyJsonConverter<IBinary>
{
    /// <inheritdoc />
    public override void Write(VerifyJsonWriter writer, IBinary value)
    {
        var stream = value.Stream;

        stream.PushToPosition(0);
        string hash;
        try {
            using var sha256 = SHA256.Create();
            sha256.ComputeHash(stream);

            hash = Convert.ToHexString(sha256.Hash);
        }
        finally {
            stream.PopPosition();
        }

        writer.WriteStartObject();

        writer.WriteMember(value, $"0x{stream.Offset:X}", nameof(DataStream.Offset));
        writer.WriteMember(value, $"0x{stream.Position:X}", nameof(Stream.Position));
        writer.WriteMember(value, $"0x{stream.Length:X}", nameof(Stream.Length));
        writer.WriteMember(value, hash, "SHA256");

        writer.WriteEndObject();
    }
}
