namespace SceneGate.Hinox.Audio;

using Yarhl.IO;

/// <summary>
/// Analyzer of binary data with potential VAG format content.
/// </summary>
/// <remarks>Only standard PSX VAG format (VAGp) supported.</remarks>
public static class VagFormatAnalyzer
{
    private const string VagpFormatId = "VAGp";
    private const int VagDataOffset = 0x30;

    /// <summary>
    /// Get the length of the waveform data without header.
    /// </summary>
    /// <param name="waveform">Waveform stream.</param>
    /// <returns>Length of the audio channels without header.</returns>
    /// <remarks>
    /// It will attempt to detect if it's a VAG (valid, with format ID in the header)
    /// and if it's the case, it will skip its header. This assummes is either VAG or raw data.
    /// </remarks>
    public static long GetChannelsLength(Stream waveform)
    {
        return IsVagFormat(waveform)
            ? waveform.Length - VagDataOffset
            : waveform.Length;
    }

    /// <summary>
    /// Determine if the binary data represents a full VAG format.
    /// </summary>
    /// <param name="binary">Binary stream to analyze.</param>
    /// <returns>Value indicating whether the binary stream represents a VAG format with header.</returns>
    public static bool IsVagFormat(Stream binary)
    {
        if (binary.Length < VagDataOffset) {
            return false;
        }

        binary.Position = 0;
        var reader = new DataReader(binary) {
            Endianness = EndiannessMode.BigEndian,
        };

        // Standard case for PSX
        string formatId = reader.ReadString(4);
        if (formatId == VagpFormatId) {
            return true;
        }

        return IsVagEditVagFormat(binary);
    }

    private static bool IsVagEditVagFormat(Stream binary)
    {
        binary.Position = 0;
        var reader = new DataReader(binary) {
            Endianness = EndiannessMode.BigEndian,
        };

        // VAGEdit seems to generate valid VAG files without header
        // First, to avoid false positives validate empty format ID and version
        if (reader.ReadUInt64() != 0x00) {
            return false;
        }

        // Channel size must be set but could be empty for null raw files or empty VAG files.
        // But sample rate shouldn't be 0
        reader.Stream.Position = 0x10;
        uint sampleRate = reader.ReadUInt32();
        if (sampleRate == 0 || sampleRate > 96000) { // I really doubt it would be higher than 22k
            return false;
        }

        // Let's try to autodetect by checking the theorical channel size field
        // This assumes MONO only. Stereo formats ARE NOT supported yet.
        reader.Stream.Position = 0x0C;
        uint channelSize = reader.ReadUInt32();
        uint expectedTotalLength = channelSize + VagDataOffset;

        return expectedTotalLength == binary.Length;
    }
}
