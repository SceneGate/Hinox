namespace SceneGate.Hinox.Tests.Audio;

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SceneGate.Hinox.Audio;
using Yarhl.FileSystem;
using Yarhl.IO;

[TestFixture]
public class Container2BinaryVabBodyTests
{
    private static IEnumerable<TestCaseData> VbWritingTestFiles =>
        TestDataBase.ReadTestCaseDataListFile(TestDataBase.VabResources, "vb_write.txt");

    [TestCaseSource(nameof(VbWritingTestFiles))]
    public void WriteIdenticalFormat(string vbName, string vhName)
    {
        string vbPath = Path.Combine(TestDataBase.VabResources, "Snapshots", vbName);
        string vhPath = Path.Combine(TestDataBase.VabResources, "Snapshots", vhName);
        TestDataBase.IgnoreIfFileDoesNotExist(vbPath);
        TestDataBase.IgnoreIfFileDoesNotExist(vhPath);

        using var originalBinary = new BinaryFormat(vbPath, FileOpenMode.Read);

        using var vbBinary = new BinaryFormat(vbPath, FileOpenMode.Read);
        using var vhBinary = new BinaryFormat(vhPath, FileOpenMode.Read);
        var header = new Binary2VabHeader().Convert(vhBinary);
        var deserialized = new BinaryVabBody2Container(header).Convert(vbBinary);

        BinaryFormat newBinary = new Container2BinaryVabBody().Convert(deserialized);

        bool identical = newBinary.Stream.Compare(originalBinary.Stream);
        Assert.That(identical, Is.True, "Streams are different");
    }

    [Test]
    public void WriteWithNonBinaryNodesThrowsException()
    {
        NodeContainerFormat container = new();

        container.Root.Add(new Node("invalid", new NodeContainerFormat()));

        var converter = new Container2BinaryVabBody();
        Assert.That(() => converter.Convert(container), Throws.InstanceOf<FormatException>());
    }

    [Test]
    public void WriteWithHeaderIgnoresIt()
    {
        NodeContainerFormat container = new();

        container.Root.Add(new Node("header", new VabHeader()));

        var converter = new Container2BinaryVabBody();
        using var result = converter.Convert(container);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void WriteMoreNodesThanLimitThrowsException()
    {
        NodeContainerFormat container = new();

        for (int i = 0; i < VabHeader.MaximumWaveforms + 1; i++) {
            container.Root.Add(new Node($"{i}", new BinaryFormat()));
        }

        var converter = new Container2BinaryVabBody();
        Assert.That(() => converter.Convert(container), Throws.InstanceOf<FormatException>());
    }

    [Test]
    public void WriteTotalBodyLargerThanLimitThrowsException()
    {
        NodeContainerFormat container = new();

        using var largeStream = new DataStream();
        var writer = new DataWriter(largeStream);
        writer.WriteTimes(0, VabHeader.MaximumTotalWaveformsSize + 1);

        container.Root.Add(new Node("0", new BinaryFormat(largeStream)));

        var converter = new Container2BinaryVabBody();
        Assert.That(() => converter.Convert(container), Throws.InstanceOf<FormatException>());
    }
}
