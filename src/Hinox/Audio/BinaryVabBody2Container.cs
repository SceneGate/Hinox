namespace SceneGate.Hinox.Audio;

using System;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

/// <summary>
/// Unpack the binary body of a VAB container (.VB) containing waveforms.
/// It requires the VAB header.
/// </summary>
public class BinaryVabBody2Container : IConverter<IBinary, NodeContainerFormat>
{
    private readonly VabHeader header;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryVabBody2Container"/> class.
    /// </summary>
    /// <param name="header">Header of the matching VAB body.</param>
    public BinaryVabBody2Container(VabHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        this.header = header;
    }

    /// <inheritdoc />
    public NodeContainerFormat Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var container = new NodeContainerFormat();

        long currentOffset = 0;
        for (int i = 0; i < header.WaveformSizes.Count; i++) {
            int fileSize = header.WaveformSizes[i];
            var waveformData = new BinaryFormat(source.Stream, currentOffset, fileSize);
            var waveformNode = new Node($"audio{i:D4}.adpcm", waveformData);
            container.Root.Add(waveformNode);
        }

        return container;
    }
}
