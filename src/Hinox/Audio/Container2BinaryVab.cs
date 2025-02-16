namespace SceneGate.Hinox.Audio;

using System;
using System.Linq;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

/// <summary>
/// Converter for writing a VAB format from a node container.
/// </summary>
public class Container2BinaryVab : IConverter<NodeContainerFormat, BinaryFormat>
{
    private readonly bool autodetectVag;

    /// <summary>
    /// Initializes a new instance of the <see cref="Container2BinaryVab"/> class
    /// without VAG autodetection.
    /// </summary>
    public Container2BinaryVab()
    {
        autodetectVag = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Container2BinaryVab"/> class.
    /// </summary>
    /// <param name="autodetectVag">
    /// Indicates whether to autodetect and remove VAG header in audio files.
    /// </param>
    public Container2BinaryVab(bool autodetectVag)
    {
        this.autodetectVag = autodetectVag;
    }

    /// <inheritdoc />
    public BinaryFormat Convert(NodeContainerFormat source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Update wave sizes before writing anything
        VabHeader header = GetHeader(source);
        UpdateFileSizes(header, source, autodetectVag);

        var vabBinary = new BinaryFormat();

        using var vhBinary = new VabHeader2Binary().Convert(header);
        vhBinary.Stream.WriteTo(vabBinary.Stream);

        using var vbBinary = new Container2BinaryVabBody(autodetectVag).Convert(source);
        vbBinary.Stream.WriteTo(vabBinary.Stream);

        return vabBinary;
    }

    /// <summary>
    /// Update the waveform sizes of a VAB header from the audios of a container.
    /// </summary>
    /// <param name="header">The VH format to update.</param>
    /// <param name="source">The container with audios.</param>
    /// <param name="autodetectVag">Value indicating whether to autodetect and remove VAG header.</param>
    /// <remarks>
    /// The nodes in the container must be binary or VabHeader (ignored).
    /// </remarks>
    public static void UpdateFileSizes(VabHeader header, NodeContainerFormat source, bool autodetectVag)
    {
        header.WaveformSizes.Clear();
        foreach (Node child in source.Root.Children) {
            if (child.Format is VabHeader) {
                continue;
            }

            if (child.Format is not IBinary binChild) {
                throw new FormatException($"Invalid format for child: '{child.Path}'");
            }

            long childLength = autodetectVag
                ? VagFormatAnalyzer.GetChannelsLength(binChild.Stream)
                : binChild.Stream.Length;
            header.WaveformSizes.Add((int)childLength);
        }
    }

    private static VabHeader GetHeader(NodeContainerFormat source)
    {
        VabHeader? header = source.Root.Children
            .Select(n => n.Format)
            .OfType<VabHeader>()
            .FirstOrDefault();
        if (header is null) {
            throw new FormatException("Cannot find header node");
        }

        return header;
    }
}
