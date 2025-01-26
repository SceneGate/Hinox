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
    private const int HeaderSize = 0x20;

    private const int ProgramAttributesSize = 0x10;
    private const int MaximumPrograms = 0x80;

    private const int ToneAttributesSize = 0x20;
    private const int MaximumTones = 0x10;
    private const int TonesSectionSize = MaximumTones * ToneAttributesSize;
    private const int TonesAttributesOffset = HeaderSize + (MaximumPrograms * ProgramAttributesSize);

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
        SectionsInfo sectionsInfo = ReadHeader(reader, header);

        ReadProgramsWithAttributes(reader, header, sectionsInfo);

        reader.Stream.Position = TonesAttributesOffset + (TonesSectionSize * sectionsInfo.ProgramCount);
        ReadWaveformSizes(reader, header, sectionsInfo.WaveformCount);

        return header;
    }

    private static SectionsInfo ReadHeader(DataReader reader, VabHeader header)
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

    private static void ReadProgramsWithAttributes(DataReader reader, VabHeader header, SectionsInfo sectionsInfo)
    {
        int totalTones = 0;
        int programIdx = -1;
        while (header.ProgramsAttributes.Count < sectionsInfo.ProgramCount) {
            programIdx++;
            reader.Stream.Position = HeaderSize + (programIdx * ProgramAttributesSize);

            VabProgramAttributes program = ReadProgramAttributes(reader, out int toneCount);
            program.Index = programIdx;

            // Between programs, some info are empty and doesn't count toward count in header
            if (toneCount == 0) {
                continue;
            }

            int readPrograms = header.ProgramsAttributes.Count;
            reader.Stream.Position = TonesAttributesOffset + (TonesSectionSize * readPrograms);
            var tones = ReadValidateTonesAttributes(reader, toneCount, programIdx, sectionsInfo.WaveformCount);
            foreach (VabToneAttributes tone in tones) {
                program.TonesAttributes.Add(tone);
                totalTones++;
            }

            header.ProgramsAttributes.Add(program);
        }

        if (totalTones != sectionsInfo.TotalToneCount) {
            throw new FormatException(
                $"Invalid count of tones. Read: {totalTones}, from header: {sectionsInfo.TotalToneCount}");
        }
    }

    private static IReadOnlyCollection<VabToneAttributes> ReadValidateTonesAttributes(
        DataReader reader,
        int toneCount,
        int programIdx,
        int waveformCount)
    {
        List<VabToneAttributes> tones = [];
        for (int toneIdx = 0; toneIdx < toneCount; toneIdx++) {
            // NOTE: A program has always 16 tones but the remaining ones will be empty
            VabToneAttributes tone = ReadToneAttributes(reader);

            if (tone.ProgramIndex != programIdx) {
                throw new FormatException(
                    $"Invalid program index in tone #{programIdx}/{toneIdx} -> {tone.ProgramIndex}");
            }

            if (tone.WaveformIndex < 0 || tone.WaveformIndex >= waveformCount) {
                throw new FormatException($"Invalid waveform index in tone #{programIdx}/{toneIdx}");
            }

            tones.Add(tone);
        }

        return tones;
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

    private static void ReadWaveformSizes(DataReader reader, VabHeader header, int count)
    {
        reader.ReadUInt16(); // skip fist always 0 (because 1-based indexes)
        for (int i = 0; i < count; i++) {
            int audioSize = reader.ReadUInt16() << 3;
            header.WaveformSizes.Add(audioSize);
        }
    }

    private sealed record SectionsInfo(int ProgramCount, int TotalToneCount, int WaveformCount);
}
