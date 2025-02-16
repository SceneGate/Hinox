namespace SceneGate.Hinox.Tests.Audio;

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SceneGate.Hinox.Audio;
using Yarhl.IO;

[TestFixture]
public class VabHeader2BinaryTests
{
    private static IEnumerable<TestCaseData> VhWritingTestFiles =>
        TestDataBase.ReadTestCaseDataGlobFile(TestDataBase.VabResources, "vh_write.txt");

    [TestCaseSource(nameof(VhWritingTestFiles))]
    public void WriteIdenticalFormat(string filePath)
    {
        TestDataBase.IgnoreIfFileDoesNotExist(filePath);

        using var originalBinary = new BinaryFormat(filePath, FileOpenMode.Read);

        var deserialized = new Binary2VabHeader().Convert(originalBinary);
        BinaryFormat newBinary = new VabHeader2Binary().Convert(deserialized);

        bool identical = newBinary.Stream.Compare(originalBinary.Stream);
        if (!identical) {
            string relativePath = Path.GetRelativePath(TestDataBase.VabResources, filePath);
            string failedPath = Path.Combine(TestDataBase.VabResources, "Failed", relativePath);
            newBinary.Stream.WriteTo(failedPath);
        }

        Assert.That(identical, Is.True, "Streams are different");
    }
}
