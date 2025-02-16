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
        SectionsInfo sectionsInfo = ReadHeader(reader, header);

        ReadProgramsWithAttributes(reader, header, sectionsInfo);

        reader.Stream.Position = VabHeader.TonesAttributesOffset
            + (VabHeader.TonesSectionSizePerProgram * sectionsInfo.ProgramCount);
        ReadWaveformSizes(reader, header, sectionsInfo.WaveformCount);

        ValidateFormat(header, sectionsInfo);

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
        int fullSize = reader.ReadInt32();
        header.Reserved0 = reader.ReadInt16();

        int programCount = reader.ReadUInt16();
        int toneCount = reader.ReadUInt16();
        int waveformCount = reader.ReadUInt16();

        header.MasterVolume = reader.ReadByte();
        header.MasterPan = reader.ReadByte();
        header.BankAttribute1 = reader.ReadByte();
        header.BankAttribute2 = reader.ReadByte();
        header.Reserved1 = reader.ReadInt32();

        return new SectionsInfo(fullSize, programCount, toneCount, waveformCount);
    }

    private static void ReadProgramsWithAttributes(DataReader reader, VabHeader header, SectionsInfo sectionsInfo)
    {
        int programIdx = -1;
        int validProgramsCount = 0;
        while (validProgramsCount < sectionsInfo.ProgramCount) {
            programIdx++;

            int relativeOffset = programIdx * VabHeader.ProgramAttributesSize;
            reader.Stream.Position = VabHeader.HeaderSize + relativeOffset;

            var program = ReadProgramAttributes(reader, out int toneCount);
            program.Index = programIdx;
            header.ProgramsAttributes.Add(program);

            // Between programs, some info are empty and doesn't count toward
            // count in header. Also added as their attributes are not constant
            // so it can generate an identical file later.
            if (toneCount == 0) {
                continue;
            }

            int toneRelativeOffset = VabHeader.TonesSectionSizePerProgram * validProgramsCount;
            reader.Stream.Position = VabHeader.TonesAttributesOffset + toneRelativeOffset;

            for (int toneIdx = 0; toneIdx < toneCount; toneIdx++) {
                // NOTE: A program has always 16 tones but the remaining ones will be empty
                VabToneAttributes tone = ReadToneAttributes(reader);
                program.TonesAttributes.Add(tone);
            }

            validProgramsCount++;
        }
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
        tone.Fine = reader.ReadByte();
        tone.Minimum = reader.ReadByte();
        tone.Maximum = reader.ReadByte();
        tone.VibrationWidth = reader.ReadByte();
        tone.VibrationTime = reader.ReadByte();
        tone.PortamentoWidth = reader.ReadByte();
        tone.PortamentoTime = reader.ReadByte();
        tone.PitchBendMinimum = reader.ReadByte();
        tone.PitchBendMaximum = reader.ReadByte();
        tone.Reserved0 = reader.ReadByte();
        tone.Reserved1 = reader.ReadByte();
        tone.EnvelopeSettings1 = reader.ReadInt16();
        tone.EnvelopeSettings2 = reader.ReadInt16();
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

    private static void ValidateFormat(VabHeader format, SectionsInfo sectionsInfo)
    {
        int totalTones = 0;
        foreach (VabProgramAttributes program in format.ProgramsAttributes) {

            for (int toneIdx = 0; toneIdx < program.TonesAttributes.Count; toneIdx++) {
                VabToneAttributes tone = program.TonesAttributes[toneIdx];
                totalTones++;

                if (tone.ProgramIndex != program.Index) {
                    throw new FormatException(
                        $"Unxpected program index in tone #{program.Index}/{toneIdx} -> {tone.ProgramIndex}");
                }

                if (tone.WaveformIndex < 0 || tone.WaveformIndex >= sectionsInfo.WaveformCount) {
                    throw new FormatException($"Unexpected waveform index in tone #{program.Index}/{toneIdx}");
                }
            }
        }

        if (totalTones != sectionsInfo.TotalToneCount) {
            throw new FormatException(
                $"Unexpected count of tones. " +
                $"Read: {totalTones}. Header: {sectionsInfo.TotalToneCount}");
        }

        int actualSize = format.GetVabSize();
        if (actualSize != sectionsInfo.FullSize) {
            throw new FormatException(
                $"Unexpected VAB size. " +
                $"Expected: {actualSize}. Header: {sectionsInfo.FullSize}");
        }
    }

    private sealed record SectionsInfo(
        int FullSize,
        int ProgramCount,
        int TotalToneCount,
        int WaveformCount);
}
