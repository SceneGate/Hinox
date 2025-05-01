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
    private readonly bool autodetectVag;

    /// <summary>
    /// Initializes a new instance of the <see cref="Container2BinaryVab"/> class
    /// without VAG autodetection.
    /// </summary>
    public Container2BinaryVabBody()
    {
        autodetectVag = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Container2BinaryVab"/> class.
    /// </summary>
    /// <param name="autodetectVag">
    /// Indicates whether to autodetect and remove VAG header in audio files.
    /// </param>
    public Container2BinaryVabBody(bool autodetectVag)
    {
        this.autodetectVag = autodetectVag;
    }

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

            if (!autodetectVag) {
                binChild.Stream.WriteTo(binary.Stream);
            } else {
                long channelsLength = VagFormatAnalyzer.GetChannelsLength(binChild.Stream);
                long dataOffset = binChild.Stream.Length - channelsLength;
                binChild.Stream.WriteSegmentTo(dataOffset, binary.Stream);
            }
        }

        if (binary.Stream.Length > VabHeader.MaximumTotalWaveformsSize) {
            throw new FormatException("Total audio length is larger than maximum supported");
        }

        return binary;
    }
}
