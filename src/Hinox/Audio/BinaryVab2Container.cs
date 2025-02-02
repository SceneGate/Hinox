namespace SceneGate.Hinox.Audio;

using System;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

/// <summary>
/// Converter for reading a VAB from its standard binary format into
/// a container with the header and audios nodes.
/// </summary>
public class BinaryVab2Container : IConverter<IBinary, NodeContainerFormat>
{
    /// <inheritdoc />
    public NodeContainerFormat Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var binaryHeader = new BinaryFormat(source.Stream);
        VabHeader header = new Binary2VabHeader().Convert(binaryHeader);

        int vbOffset = header.GetHeaderSize();
        long vbLength = source.Stream.Length - vbOffset;
        using var binaryBody = new BinaryFormat(source.Stream, vbOffset, vbLength);
        NodeContainerFormat container = new BinaryVabBody2Container(header).Convert(binaryBody);

        container.Root.Add(new Node("header", header));

        return container;
    }
}
