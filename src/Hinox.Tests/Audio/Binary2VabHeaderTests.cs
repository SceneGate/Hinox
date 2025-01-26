namespace SceneGate.Hinox.Tests.Audio;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SceneGate.Hinox.Audio;
using SceneGate.Hinox.Tests.Framework;
using VerifyNUnit;
using Yarhl.FileSystem;
using Yarhl.IO;

[TestFixture]
public class Binary2VabHeaderTests
{
    private static IEnumerable VhSnapshotTestFiles =>
        TestDataBase.ReadTestCaseDataListFile(TestDataBase.VabResources, "vh_snapshots.txt");

    private static IEnumerable<TestCaseData> VhReadingTestFiles =>
        TestDataBase.ReadTestCaseDataGlobFile(TestDataBase.VabResources, "vh_read.txt");

    [TestCaseSource(nameof(VhSnapshotTestFiles))]
    public Task VerifyDeserializationForSnapshots(string filename)
    {
        string filePath = Path.Combine(TestDataBase.VabResources, "Snapshots", filename);
        TestDataBase.IgnoreIfFileDoesNotExist(filePath);

        using Node testNode = NodeFactory.FromFile(filePath, FileOpenMode.Read)
            .TransformWith<Binary2VabHeader>();
        VabHeader actual = testNode.GetFormatAs<VabHeader>();

        Verifier.UseProjectRelativeDirectory("Resources/VAB/Snapshots");
        return Verifier.Verify(actual)
            .UseFileName(filename)
            .AddExtraSettings(x => {
                x.DefaultValueHandling = Argon.DefaultValueHandling.Include;
                x.Converters.Add(new HexadecimalVerifierJsonConverter<VabHeader>());
            });
    }

    [TestCaseSource(nameof(VhReadingTestFiles))]
    public void ValidateAllFiles(string filePath)
    {
        TestDataBase.IgnoreIfFileDoesNotExist(filePath);

        using Node testNode = NodeFactory.FromFile(filePath, FileOpenMode.Read);

        Assert.That(testNode.TransformWith<Binary2VabHeader>, Throws.Nothing);
        Assert.That(testNode.Format, Is.TypeOf<VabHeader>());
    }
}
