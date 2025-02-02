namespace SceneGate.Hinox.Audio;

using System;
using System.Diagnostics.CodeAnalysis;
using Yarhl.FileFormat;
using Yarhl.IO;

/// <summary>
/// Converter for writing VAB header (VH) into its standard binary format.
/// </summary>
public class VabHeader2Binary : IConverter<VabHeader, BinaryFormat>
{
    private static readonly int[] supportedVersions = [5, 6, 7];

    /// <inheritdoc />
    public BinaryFormat Convert(VabHeader source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var binary = new BinaryFormat();
        var writer = new DataWriter(binary.Stream) {
            Endianness = EndiannessMode.LittleEndian,
        };

        if (!supportedVersions.Contains(source.Version)) {
            throw new NotSupportedException("Unsupported VH version");
        }

        WriteHeader(writer, source);
        WriteProgramsAttributes(writer, source);
        WriteTonesAttributes(writer, source);
        WriteWaveformSizes(writer, source);

        return binary;
    }

    private static void WriteHeader(DataWriter writer, VabHeader format)
    {
        int programCount = format.ProgramsAttributes.Count(p => p.TonesAttributes.Count > 0);
        int toneCount = format.ProgramsAttributes.Sum(p => p.TonesAttributes.Count);
        int waveformsCount = format.WaveformSizes.Count;

        writer.Write(VabHeader.FormatId, nullTerminator: false);
        writer.Write(format.Version);
        writer.Write(format.VabId);
        writer.Write(format.GetVabSize());
        writer.Write(format.Reserved0);
        writer.Write((ushort)programCount);
        writer.Write((ushort)toneCount);
        writer.Write((ushort)waveformsCount);
        writer.Write(format.MasterVolume);
        writer.Write(format.MasterPan);
        writer.Write(format.BankAttribute1);
        writer.Write(format.BankAttribute2);
        writer.Write(format.Reserved1);
    }

    private static void WriteProgramsAttributes(DataWriter writer, VabHeader format)
    {
        if (format.ProgramsAttributes.Count > 0 && format.ProgramsAttributes[0].Index != 0) {
            throw new FormatException("First program must have index 0");
        }

        foreach (VabProgramAttributes program in format.ProgramsAttributes) {
            WriteProgramAttributes(writer, program);
        }

        VabProgramAttributes lastProgram = format.ProgramsAttributes.Count == 0
            ? new VabProgramAttributes() // constructor sets default properties
            : format.ProgramsAttributes[^1];

        int finalEmptyCount = VabHeader.MaximumPrograms - (lastProgram.Index + 1);
        for (int m = 0; m < finalEmptyCount; m++) {
            WriteEmptyProgramAttributes(writer, format.Version, lastProgram);
        }
    }

    private static void WriteProgramAttributes(DataWriter writer, VabProgramAttributes program)
    {
        writer.Write((byte)program.TonesAttributes.Count);
        writer.Write(program.MasterVolume);
        writer.Write(program.Priority);
        writer.Write(program.Mode);
        writer.Write(program.MasterPanning);
        writer.Write(program.Reserved0);
        writer.Write(program.Attributes);
        writer.Write(program.Reserved1);
        writer.Write(program.Reserved2);
    }

    private static void WriteEmptyProgramAttributes(DataWriter writer, int version, VabProgramAttributes reference)
    {
        bool cleanMasterValues = version >= 6;

        writer.Write((byte)0);
        writer.Write(cleanMasterValues ? (byte)0 : reference.MasterVolume);
        writer.Write(reference.Priority);
        writer.Write(reference.Mode);
        writer.Write(cleanMasterValues ? (byte)0 : reference.MasterPanning);
        writer.Write(reference.Reserved0);
        writer.Write(reference.Attributes);
        writer.Write(reference.Reserved1);
        writer.Write(reference.Reserved2);
    }

    [SuppressMessage("", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "Readability")]
    private static void WriteTonesAttributes(DataWriter writer, VabHeader format)
    {
        foreach (VabProgramAttributes program in format.ProgramsAttributes) {
            if (program.TonesAttributes.Count == 0) {
                continue;
            }

            foreach (VabToneAttributes tone in program.TonesAttributes) {
                WriteToneAttributes(writer, tone);
            }

            int finalEmptyCount =  VabHeader.MaximumTones - program.TonesAttributes.Count;
            for (int t = 0; t < finalEmptyCount; t++) {
                WriteEmptyToneAttributes(writer, format.Version, program.TonesAttributes[^1]);
            }
        }
    }

    private static void WriteToneAttributes(DataWriter writer, VabToneAttributes tone)
    {
        writer.Write(tone.Priority);
        writer.Write(tone.Mode);
        writer.Write(tone.Volume);
        writer.Write(tone.Panning);
        writer.Write(tone.Centre);
        writer.Write(tone.Fine);
        writer.Write(tone.Minimum);
        writer.Write(tone.Maximum);
        writer.Write(tone.VibrationWidth);
        writer.Write(tone.VibrationTime);
        writer.Write(tone.PortamentoWidth);
        writer.Write(tone.PortamentoTime);
        writer.Write(tone.PitchBendMinimum);
        writer.Write(tone.PitchBendMaximum);
        writer.Write(tone.Reserved0);
        writer.Write(tone.Reserved1);
        writer.Write(tone.EnvelopeSettings1);
        writer.Write(tone.EnvelopeSettings2);
        writer.Write(tone.ProgramIndex);
        writer.Write((ushort)(tone.WaveformIndex + 1));
        writer.Write(tone.Reserved2);
    }

    private static void WriteEmptyToneAttributes(DataWriter writer, int version, VabToneAttributes reference)
    {
        bool cleanAttributes = version >= 6;
        if (cleanAttributes) {
            writer.WriteTimes(0, 14);
        } else {
            writer.Write(reference.Priority);
            writer.Write(reference.Mode);
            writer.Write(reference.Volume);
            writer.Write(reference.Panning);
            writer.Write(reference.Centre);
            writer.Write(reference.Fine);
            writer.Write((byte)0); // min
            writer.Write((byte)0); // max
            writer.Write(reference.VibrationWidth);
            writer.Write(reference.VibrationTime);
            writer.Write(reference.PortamentoWidth);
            writer.Write(reference.PortamentoTime);
            writer.Write(reference.PitchBendMinimum);
            writer.Write(reference.PitchBendMaximum);
        }

        writer.Write(reference.Reserved0);
        writer.Write(reference.Reserved1);

        if (version == 5) {
            writer.Write(reference.EnvelopeSettings1);
            writer.Write(reference.EnvelopeSettings2);
        } else if (version == 6) {
            writer.Write((uint)0);
        } else if (version >= 7) {
            writer.Write((ushort)0x80FF);
            writer.Write((ushort)0x5FC0);
        }

        writer.Write(reference.ProgramIndex);

        bool cleanWaveformIndex = version >= 6;
        writer.Write(cleanWaveformIndex ? (ushort)0 : (ushort)(reference.WaveformIndex + 1));

        writer.Write(reference.Reserved2);
    }

    private static void WriteWaveformSizes(DataWriter writer, VabHeader format)
    {
        writer.Write((ushort)0);
        foreach (int waveformSize in format.WaveformSizes) {
            ushort encoded = (ushort)(waveformSize >> 3);
            writer.Write(encoded);
        }

        int currentSize = (format.WaveformSizes.Count + 1) * 2;
        int padding = VabHeader.WaveformsSizeSectionSize - currentSize;
        writer.WriteTimes(0, padding);
    }
}
