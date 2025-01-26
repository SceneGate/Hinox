namespace SceneGate.Hinox.Audio;

using System.Collections.ObjectModel;

public class VabProgramAttributes
{
    /// <summary>
    /// Gets or sets the index of the program in the VAB format.
    /// </summary>
    public int Index { get; set; }

    public byte MasterVolume { get; set; }

    public byte Priority { get; set; }

    public byte Mode { get; set; }

    public byte MasterPanning { get; set; }

    public byte Reserved0 { get; set; }

    public short Attributes { get; set; }

    public int Reserved1 { get; set; }

    public int Reserved2 { get; set; }

    public Collection<VabToneAttributes> TonesAttributes { get; init; } = [];
}
