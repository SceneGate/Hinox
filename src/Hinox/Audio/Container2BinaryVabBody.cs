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
    private const string VagFormatId = "pGAV";
    private const int VagDataOffset = 0x30;

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

            if (IsVagFormat(binChild.Stream)) {
                binChild.Stream.WriteSegmentTo(VagDataOffset, binary.Stream);
            } else {
                binChild.Stream.WriteTo(binary.Stream);
            }
        }

        if (binary.Stream.Length > VabHeader.MaximumTotalWaveformsSize) {
            throw new FormatException("Total audio length is larger than maximum supported");
        }

        return binary;
    }

    /// <summary>
    /// Get the length of the waveform data that will be written in the VAB body.
    /// </summary>
    /// <param name="waveform">Waveform stream.</param>
    /// <returns>Length of the given stream to be written.</returns>
    /// <remarks>
    /// It will attempt to detect if it's a VAG (valid, with format ID in the header)
    /// and if it's the case, it will skip its header.
    /// </remarks>
    public static long GetWaveformLength(Stream waveform)
    {
        return IsVagFormat(waveform)
            ? waveform.Length - VagDataOffset
            : waveform.Length;
    }

    private static bool IsVagFormat(Stream binary)
    {
        if (binary.Length == 0) {
            return false;
        }

        binary.Position = 0;
        var reader = new DataReader(binary);
        return reader.ReadString(4) == VagFormatId;
    }
}
