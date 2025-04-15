namespace SceneGate.Hinox.Audio;

using System;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

/// <summary>
/// Converter for reading a VAB from its standard binary format into
/// a container with the header and audios nodes.
/// </summary>
/// <remarks>
/// The container contains a node 'header' with the VH content in
/// <see cref="VabHeader"/> format.
/// </remarks>
public class BinaryVab2Container : IConverter<IBinary, NodeContainerFormat>
{
        /// <summary>
    /// Initializes a new instance of the <see cref="BinaryVab2Container"/> class.
    /// </summary>
    public BinaryVab2Container()
    {
        IncludePaddingTones = false;
        ThrowOnInvalidData = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryVab2Container"/> class.
    /// </summary>
    /// <param name="includePaddingTones">
    /// Value that indicates whether to include empty tones.
    /// </param>
    /// <param name="throwOnInvalid">
    /// Value indicating whether to throw exceptions on unexpected data.
    /// </param>
    public BinaryVab2Container(bool includePaddingTones, bool throwOnInvalid)
    {
        IncludePaddingTones = includePaddingTones;
        ThrowOnInvalidData = throwOnInvalid;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include empty tones without
    /// a valid waveform links.
    /// </summary>
    /// <remarks>
    /// Setting to true allows to recreate the original header even when it
    /// doesn't follow standard rules, but the exported content would be more
    /// verbosed.
    /// </remarks>
    public bool IncludePaddingTones { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to throw exceptions if the read
    /// data contains unexpected information that doesn't match the specs.
    /// </summary>
    /// <remarks>
    /// Some games didn't generate full complaint files but valid enough to work.
    /// </remarks>
    public bool ThrowOnInvalidData { get; set; }

    /// <inheritdoc />
    public NodeContainerFormat Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var binaryHeader = new BinaryFormat(source.Stream, 0, source.Stream.Length);
        VabHeader header = new Binary2VabHeader(IncludePaddingTones, ThrowOnInvalidData)
            .Convert(binaryHeader);

        int vbOffset = header.GetHeaderSize();
        long vbLength = source.Stream.Length - vbOffset;
        using var binaryBody = new BinaryFormat(source.Stream, vbOffset, vbLength);
        NodeContainerFormat container = new BinaryVabBody2Container(header).Convert(binaryBody);

        container.Root.Add(new Node("header", header));

        return container;
    }
}
