namespace SceneGate.Hinox.Audio;

using System.Collections.ObjectModel;
using Yarhl.FileFormat;

/// <summary>
/// Represents the header of a VAB file format (.VAB), also found as separate
/// files with .VH extension.
/// </summary>
public class VabHeader : IFormat
{
    internal const int HeaderSize = 0x20;

    internal const int ProgramAttributesSize = 0x10;
    internal const int MaximumPrograms = 0x80;
    internal const int ProgramsSectionSize = MaximumPrograms * ProgramAttributesSize;

    internal const int ToneAttributesSize = 0x20;
    internal const int MaximumTones = 0x10;
    internal const int TonesSectionSizePerProgram = MaximumTones * ToneAttributesSize;
    internal const int TonesAttributesOffset = HeaderSize + ProgramsSectionSize;

    internal const int WaveformsSizeSectionSize = 0x200;

    /// <summary>
    /// Gets the ID or magic stamp of the binary format.
    /// </summary>
    /// <remarks>VABp as little-endian.</remarks>
    public static string FormatId => "pBAV";

    /// <summary>
    /// Gets or sets the version of the format.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the ID of the VAB file.
    /// </summary>
    public int VabId { get; set; }

    /// <summary>
    /// Gets or sets a reserved field at position 0x10.
    /// </summary>
    public short Reserved0 { get; set; }

    /// <summary>
    /// Gets or sets the master volume of the VAB audios.
    /// </summary>
    public byte MasterVolume { get; set; }

    /// <summary>
    /// Gets or sets the master pan of the VAB audios.
    /// </summary>
    public byte MasterPan { get; set; }

    /// <summary>
    /// Gets or sets the first user-defined bank attributes.
    /// </summary>
    public byte BankAttribute1 { get; set; }

    /// <summary>
    /// Gets or sets the second user-defined bank attributes.
    /// </summary>
    public byte BankAttribute2 { get; set; }

    /// <summary>
    /// Gets or sets a reserved field at position 0x1C.
    /// </summary>
    public int Reserved1 { get; set; }

    /// <summary>
    /// Gets the collection of programs attributes with their tones attributes.
    /// </summary>
    public Collection<VabProgramAttributes> ProgramsAttributes { get; init; } = [];

    /// <summary>
    /// Gets the collection of file length of the waveform audios in the container.
    /// </summary>
    public Collection<int> WaveformSizes { get; } = [];

    /// <summary>
    /// Gets the size of the VH header format.
    /// </summary>
    /// <returns></returns>
    public int GetHeaderSize()
    {
        int validPrograms = ProgramsAttributes.Count(p => p.TonesAttributes.Count > 0);
        return HeaderSize
            + ProgramsSectionSize
            + (TonesSectionSizePerProgram * validPrograms)
            + WaveformsSizeSectionSize;
    }

    /// <summary>
    /// Gets the full size of the VAB file (header and body).
    /// </summary>
    /// <returns>The full size of the VAB format.</returns>
    public int GetVabSize()
    {
        int bodySize = WaveformSizes.Sum();
        return GetHeaderSize() + bodySize;
    }
}
