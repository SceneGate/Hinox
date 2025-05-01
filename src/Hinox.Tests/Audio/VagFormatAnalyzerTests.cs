namespace SceneGate.Hinox.Tests.Audio;

using NUnit.Framework;
using SceneGate.Hinox.Audio;
using Yarhl.IO;

[TestFixture]
public class VagFormatAnalyzerTests
{
    [Test]
    public void GetChannelsLengthWithRawAudioReturnsStreamLength()
    {
        using var audio = new DataStream();
        var writer = new DataWriter(audio);

        // Random data that may look like SPU ADPCM
        writer.WriteTimes(0x00, 16);
        writer.Write([0x16, 0x00, 0x42, 0x30, 0xE2, 0x2E, 0x01, 0x2F, 0xD1, 0x40, 0x02, 0x21, 0xBD, 0x02, 0xFB, 0x23]);

        long actualLength = VagFormatAnalyzer.GetChannelsLength(audio);

        Assert.That(actualLength, Is.EqualTo(audio.Length));
    }

    [Test]
    public void GetChannelsLengthWithPsxVagpSkipHeader()
    {
        const int HeaderLength = 0x30;
        using var audio = new DataStream();
        var writer = new DataWriter(audio);

        // Just the format ID for quick detection trigger
        writer.Write("VAGp", nullTerminator: false);
        writer.WriteUntilLength(0x00, HeaderLength);

        // Random data that may look like SPU ADPCM
        writer.WriteTimes(0x00, 16);
        writer.Write([0x16, 0x00, 0x42, 0x30, 0xE2, 0x2E, 0x01, 0x2F, 0xD1, 0x40, 0x02, 0x21, 0xBD, 0x02, 0xFB, 0x23]);

        long actualLength = VagFormatAnalyzer.GetChannelsLength(audio);

        Assert.That(actualLength, Is.EqualTo(audio.Length - HeaderLength));
    }

    [Test]
    public void GetChannelsLengthWithVagEditGeneratedVag()
    {
        const int HeaderLength = 0x30;
        const int ChannelsLength = 0x20;

        using var audio = new DataStream();
        var writer = new DataWriter(audio) {
            Endianness = EndiannessMode.BigEndian,
        };

        // Only the channels length and sample rate
        writer.WriteTimes(0x00, 0x0C);
        writer.Write(ChannelsLength);
        writer.Write(11_025);
        writer.WriteUntilLength(0x00, HeaderLength);

        // Random data that may look like SPU ADPCM
        writer.WriteTimes(0x00, 16);
        writer.Write([0x16, 0x00, 0x42, 0x30, 0xE2, 0x2E, 0x01, 0x2F, 0xD1, 0x40, 0x02, 0x21, 0xBD, 0x02, 0xFB, 0x23]);

        long actualLength = VagFormatAnalyzer.GetChannelsLength(audio);

        Assert.That(actualLength, Is.EqualTo(ChannelsLength));
    }

    [Test]
    public void GetChannelsLengthWithEmptyFileReturnsAsFullAudio()
    {
        using var audio = new DataStream();
        var writer = new DataWriter(audio) {
            Endianness = EndiannessMode.BigEndian,
        };

        writer.WriteTimes(0x00, 0x30);

        long actualLength = VagFormatAnalyzer.GetChannelsLength(audio);

        Assert.That(actualLength, Is.EqualTo(0x30));
    }
}
