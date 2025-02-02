namespace SceneGate.Hinox.Tests.Audio;

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SceneGate.Hinox.Audio;
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
}
