namespace SceneGate.Hinox.Audio;

using System;
using System.Linq;
using Yarhl.FileFormat;
using Yarhl.IO;

/// <summary>
/// Converter for reading VAB header (VH) from its standard binary format.
/// </summary>
public class Binary2VabHeader : IConverter<IBinary, VabHeader>
{
    private static readonly int[] supportedVersions = [5, 6, 7];

    /// <inheritdoc />
    public VabHeader Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        source.Stream.Position = 0;
        var reader = new DataReader(source.Stream) {
            Endianness = EndiannessMode.LittleEndian,
        };

        var header = new VabHeader();
        SectionsInfo sectionsInfo = ReadGeneralInfo(reader, header);

        // TODO: Improve handling of empty entries
        // TODO: Store empty entries info to generate 100% same file
        int tonesRead = 0;
        for (int p = 0, programsRead = 0; programsRead < sectionsInfo.ProgramCount; p++) {
            source.Stream.Position = 0x20 + (p * 0x10);
            VabProgramAttributes program = ReadProgramAttributes(reader, out int toneCount);
            if (toneCount == 0) {
                // Between programs, some info are empty and doesn't count toward count in header
                continue;
            }

            source.Stream.Position = 0x820 + (0x200 * programsRead);
            for (int t = 0; t < toneCount; t++) {
                // After program tones, it keeps saying it's for the same program but they are empty only some fields set
                VabToneAttributes tone = ReadToneAttributes(reader);

                if (tone.ProgramIndex != p) {
                    throw new FormatException($"Invalid program index in tone #{p}/{t} -> {tone.ProgramIndex}");
                }

                if (tone.WaveformIndex < 0 || tone.WaveformIndex >= sectionsInfo.WaveformCount) {
                    throw new FormatException($"Invalid waveform index in tone #{p}/{t}");
                }

                program.TonesAttributes.Add(tone);
                tonesRead++;
            }

            programsRead++;
            header.ProgramsAttributes.Add(program);
        }

        if (header.ProgramsAttributes.Sum(p => p.TonesAttributes.Count) != sectionsInfo.TotalToneCount) {
            throw new FormatException(
                $"Invalid count of tones. Read: {tonesRead}, from header: {sectionsInfo.TotalToneCount}");
        }

        reader.Stream.Position = 0x820 + (0x200 * sectionsInfo.ProgramCount);
        reader.ReadUInt16(); // skip fist always 0 (because 1-based indexes)
        for (int i = 0; i < sectionsInfo.WaveformCount; i++) {
            int audioSize = reader.ReadUInt16() << 3;
            header.WaveformSizes.Add(audioSize);
        }

        return header;
    }

    private static SectionsInfo ReadGeneralInfo(DataReader reader, VabHeader header)
    {
        string fileId = reader.ReadString(4);
        if (fileId != VabHeader.FormatId) {
            throw new FormatException($"Invalid format ID '{fileId}'");
        }

        header.Version = reader.ReadInt32();
        if (!supportedVersions.Contains(header.Version)) {
            throw new NotSupportedException($"Unsupported format version 0x{header.Version:X}");
        }

        header.VabId = reader.ReadInt32();
        header.FullSize = reader.ReadInt32();
        header.Reserved0 = reader.ReadInt16();

        int programCount = reader.ReadUInt16();
        int toneCount = reader.ReadUInt16();
        int waveformCount = reader.ReadUInt16();

        header.MasterVolume = reader.ReadByte();
        header.MasterPan = reader.ReadByte();
        header.BankAttribute1 = reader.ReadByte();
        header.BankAttribute2 = reader.ReadByte();
        header.Reserved1 = reader.ReadInt32();

        return new SectionsInfo(programCount, toneCount, waveformCount);
    }

    private static VabProgramAttributes ReadProgramAttributes(DataReader reader, out int toneCount)
    {
        var program = new VabProgramAttributes();

        toneCount = reader.ReadByte();
        program.MasterVolume = reader.ReadByte();
        program.Priority = reader.ReadByte();
        program.Mode = reader.ReadByte();
        program.MasterPanning = reader.ReadByte();
        program.Reserved0 = reader.ReadByte();
        program.Attributes = reader.ReadInt16();
        program.Reserved1 = reader.ReadInt32();
        program.Reserved2 = reader.ReadInt32();

        return program;
    }

    private static VabToneAttributes ReadToneAttributes(DataReader reader)
    {
        var tone = new VabToneAttributes();

        tone.Priority = reader.ReadByte();
        tone.Mode = reader.ReadByte();
        tone.Volume = reader.ReadByte();
        tone.Panning = reader.ReadByte();
        tone.Centre = reader.ReadByte();
        tone.Shift = reader.ReadByte();
        tone.Minimum = reader.ReadByte();
        tone.Maximum = reader.ReadByte();
        tone.VibW = reader.ReadByte();
        tone.VibT = reader.ReadByte();
        tone.PorW = reader.ReadByte();
        tone.PorT = reader.ReadByte();
        tone.PbMin = reader.ReadByte();
        tone.PbMax = reader.ReadByte();
        tone.Reserved0 = reader.ReadByte();
        tone.Reserved1 = reader.ReadByte();
        tone.Adsr1 = reader.ReadInt16();
        tone.Adsr2 = reader.ReadInt16();
        tone.ProgramIndex = reader.ReadInt16();
        tone.WaveformIndex = (short)(reader.ReadInt16() - 1); // 1-based index
        tone.Reserved2 = reader.ReadInt64();

        return tone;
    }

    private sealed record SectionsInfo(int ProgramCount, int TotalToneCount, int WaveformCount);
}
