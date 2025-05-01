namespace SceneGate.Hinox.Audio;

using System.Collections.ObjectModel;

/// <summary>
/// Represents the attributes of a VAB program.
/// </summary>
public class VabProgramAttributes
{
    /// <summary>
    /// Gets or sets the index of the program in the VAB format.
    /// </summary>
    public int Index { get; set; } = -1;

    /// <summary>
    /// Gets or sets the master volume of the tones in the program.
    /// </summary>
    public byte MasterVolume { get; set; }

    /// <summary>
    /// Gets or sets the priority of the tones.
    /// </summary>
    public byte Priority { get; set; }

    /// <summary>
    /// Gets or sets the mode.
    /// </summary>
    public byte Mode { get; set; } = 0xE0;

    /// <summary>
    /// Gets or sets the master panning.
    /// </summary>
    public byte MasterPanning { get; set; }

    /// <summary>
    /// Gets or sets a reserved field at position 0x5.
    /// </summary>
    public byte Reserved0 { get; set; }

    /// <summary>
    /// Gets or sets additional attributes.
    /// </summary>
    public short Attributes { get; set; }

    /// <summary>
    /// Gets or sets a reserved field at position 0x8.
    /// </summary>
    public int Reserved1 { get; set; } = -1;

    /// <summary>
    /// Gets or sets a reserved field at position 0xC.
    /// </summary>
    public int Reserved2 { get; set; } = -1;

    /// <summary>
    /// Gets the collection of tones attributes in the program.
    /// </summary>
    public Collection<VabToneAttributes> TonesAttributes { get; init; } = [];
}
