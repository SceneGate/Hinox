namespace SceneGate.Hinox.Audio;

/// <summary>
/// Represents the attributes of tones for a program in a VAB audio container.
/// </summary>
public class VabToneAttributes
{
    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public byte Priority { get; set; }

    /// <summary>
    /// Gets or sets the mode.
    /// </summary>
    /// <remarks>0: normal, 4: reverberation.</remarks>
    public byte Mode { get; set; }

    /// <summary>
    /// Gets or sets the tone volume.
    /// </summary>
    public byte Volume { get; set; }

    /// <summary>
    /// Gets or sets the tone panning.
    /// </summary>
    public byte Panning { get; set; }

    /// <summary>
    /// Gets or sets the centre note (in semitone units).
    /// </summary>
    public byte Centre {get; set; }

    /// <summary>
    /// Gets or sets the centre note fine tuning for pitch correction.
    /// </summary>
    public byte Fine { get; set; }

    /// <summary>
    /// Gets or sets the note minimum value.
    /// </summary>
    public byte Minimum { get; set; }

    /// <summary>
    /// Gets or sets the note maximum value.
    /// </summary>
    public byte Maximum { get; set; }

    /// <summary>
    /// Gets or sets the vibration width.
    /// </summary>
    public byte VibW { get; set; }

    /// <summary>
    /// Gets or sets the vibration duration.
    /// </summary>
    public byte VibT { get; set; }

    /// <summary>
    /// Gets or sets the portamento width.
    /// </summary>
    public byte PorW { get; set; }

    /// <summary>
    /// Gets or sets the portamento duration.
    /// </summary>
    public byte PorT { get; set; }

    /// <summary>
    /// Gets or sets the minimum pitch bend.
    /// </summary>
    public byte PbMin { get; set; }

    /// <summary>
    /// Gets or sets the maximum pitch bend.
    /// </summary>
    public byte PbMax { get; set; }

    /// <summary>
    /// Gets or sets a reserved field at position 0xE.
    /// </summary>
    public byte Reserved0 { get; set; }

    /// <summary>
    /// Gets or sets a reserved field at position 0xF.
    /// </summary>
    public byte Reserved1 { get; set; }

    /// <summary>
    /// Gets or sets the settings for attack and decay.
    /// </summary>
    public short Adsr1 { get; set; }

    /// <summary>
    /// Gets or sets the settings for release and sustain.
    /// </summary>
    public short Adsr2 { get; set; }

    /// <summary>
    /// Gets or sets the 0-based index of the program of the tone.
    /// </summary>
    public short ProgramIndex { get; set; }

    /// <summary>
    /// Gets or sets the 0-based index of the waveform (VAG) audio.
    /// </summary>
    public short WaveformIndex { get; set; }

    /// <summary>
    /// Gets or sets 4 16-bits values of reserved fields at position 0x18.
    /// </summary>
    public long Reserved2 { get; set; }
}
