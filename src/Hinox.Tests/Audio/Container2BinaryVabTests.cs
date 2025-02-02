namespace SceneGate.Hinox.Tests.Audio;

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SceneGate.Hinox.Audio;
using VerifyTests;
using Yarhl.IO;

[TestFixture]
public class Container2BinaryVabTests
{
    private static IEnumerable<TestCaseData> VabWritingTestFiles =>
        TestDataBase.ReadTestCaseDataListFile(TestDataBase.VabResources, "vab_write.txt");

    [TestCaseSource(nameof(VabWritingTestFiles))]
    public void WriteIdenticalFormat(string vabName)
    {
         string vabPath = Path.Combine(TestDataBase.VabResources, "Snapshots", vabName);
        TestDataBase.IgnoreIfFileDoesNotExist(vabPath);

        using var originalBinary = new BinaryFormat(vabPath, FileOpenMode.Read);

        var deserialized = new BinaryVab2Container().Convert(originalBinary);
        using BinaryFormat newBinary = new Container2BinaryVab().Convert(deserialized);

        bool identical = newBinary.Stream.Compare(originalBinary.Stream);
        if (!identical) {
            string failedPath = Path.Combine(TestDataBase.VabResources, "Failed", vabName);
            newBinary.Stream.WriteTo(failedPath);
        }

        Assert.That(identical, Is.True, "Streams are different");
    }
}
