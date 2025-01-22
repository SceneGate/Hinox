namespace SceneGate.Hinox.Audio;

using System.Collections.ObjectModel;
using Yarhl.FileFormat;

/// <summary>
/// Represents the header of a VAB file format (.VAB), also found as separate
/// files with .VH extension.
/// </summary>
public class VabHeader : IFormat
{
    public static string FormatId => "pBAV"; // VABp as little-endian

    public int Version { get; set; }

    public int VabId { get; set; }

    public int FullSize { get; set; }

    public short Reserved0 { get; set; }

    public byte MasterVolume { get; set; }

    public byte MasterPan { get; set; }

    public byte BankAttribute1 { get; set; }

    public byte BankAttribute2 { get; set; }

    public int Reserved1 { get; set; }

    public Collection<VabProgramAttributes> ProgramsAttributes { get; init; } = [];

    public Collection<int> WaveformSizes { get; } = [];
}
