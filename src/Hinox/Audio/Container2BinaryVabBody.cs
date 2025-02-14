namespace SceneGate.Hinox.Audio;

using System;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

/// <summary>
/// Converter for writing VAB body (VB) from a container into its standard binary format.
/// </summary>
public class Container2BinaryVabBody : IConverter<NodeContainerFormat, BinaryFormat>
{
    /// <inheritdoc />
    public BinaryFormat Convert(NodeContainerFormat source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var binary = new BinaryFormat();

        int count = 0;
        foreach (Node child in source.Root.Children) {
            if (child.Format is VabHeader) {
                continue;
            }

            if (child.Format is not IBinary binChild) {
                throw new FormatException($"Invalid format for child: '{child.Path}'");
            }

            count++;
            if (count > VabHeader.MaximumWaveforms) {
                throw new FormatException("Reached maximum number of audio files");
            }

            binChild.Stream.WriteTo(binary.Stream);
        }

        if (binary.Stream.Length > VabHeader.MaximumTotalWaveformsSize) {
            throw new FormatException("Total audio length is larger than maximum supported");
        }

        return binary;
    }
}
