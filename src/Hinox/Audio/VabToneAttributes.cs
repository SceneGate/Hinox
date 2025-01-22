namespace SceneGate.Hinox.Audio;

public class VabToneAttributes
{
    public byte Priority { get; set; }

    public byte Mode { get; set; }

    public byte Volume { get; set; }

    public byte Panning { get; set; }

    public byte Centre {get; set; }

    public byte Shift { get; set; }

    public byte Minimum { get; set; }

    public byte Maximum { get; set; }

    public byte VibW { get; set; }

    public byte VibT { get; set; }

    public byte PorW { get; set; }

    public byte PorT { get; set; }

    public byte PbMin { get; set; }

    public byte PbMax { get; set; }

    public byte Reserved0 { get; set; }

    public byte Reserved1 { get; set; }

    public short Adsr1 { get; set; }

    public short Adsr2 { get; set; }

    public short ProgramIndex { get; set; }

    public short WaveformIndex { get; set; }

    public long Reserved2 { get; set; }
}
