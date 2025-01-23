namespace SceneGate.Hinox.Tests.Audio;

using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SceneGate.Hinox.Audio;
using VerifyNUnit;
using VerifyTests;
using Yarhl.FileSystem;
using Yarhl.IO;

[TestFixture]
public class Binary2VabHeaderTests
{
    private static string VabResourcesPath => Path.Combine(TestDataBase.AudioResources, "VAB");

    public static IEnumerable GetVhSnapshotTestFiles()
    {
        string listFilePath = Path.Combine(VabResourcesPath, "snapshots_vh.txt");
        return TestDataBase.ReadTestListFile(listFilePath)
            .Select(data => new TestCaseData(data));
    }

    public static IEnumerable GetAllVhTestFiles()
    {
        string listFilePath = Path.Combine(VabResourcesPath, "snapshots_vh.txt");
        foreach (string entry in TestDataBase.ReadTestListFile(listFilePath)) {
            yield return new TestCaseData(Path.Combine(VabResourcesPath, "Snapshots", entry))
                .SetArgDisplayNames(entry);
        }

        // TODO: convert to glob from txt file
        string validationPath = Path.Combine(VabResourcesPath, "Validation");
        foreach (string filePath in Directory.EnumerateFiles(validationPath, "*.vh", SearchOption.AllDirectories)) {
            yield return new TestCaseData(filePath)
                .SetArgDisplayNames(Path.GetRelativePath(validationPath, filePath));
        }
    }

    [TestCaseSource(nameof(GetVhSnapshotTestFiles))]
    public Task VerifyDeserializationForSnapshots(string relativePath)
    {
        string filePath = Path.Combine(TestDataBase.AudioResources, "VAB", "Snapshots", relativePath);
        TestDataBase.IgnoreIfFileDoesNotExist(filePath);

        using Node testNode = NodeFactory.FromFile(filePath, FileOpenMode.Read)
            .TransformWith<Binary2VabHeader>();
        VabHeader actual = testNode.GetFormatAs<VabHeader>();

        Verifier.UseProjectRelativeDirectory("Resources/Audio/VAB/Snapshots");
        return Verifier.Verify(actual)
            .UseFileName(relativePath)
            .AddExtraSettings(x => x.DefaultValueHandling = Argon.DefaultValueHandling.Include);
    }

    [TestCaseSource(nameof(GetAllVhTestFiles))]
    public void ValidateAllFiles(string filePath)
    {
        TestDataBase.IgnoreIfFileDoesNotExist(filePath);

        using Node testNode = NodeFactory.FromFile(filePath, FileOpenMode.Read);

        Assert.That(testNode.TransformWith<Binary2VabHeader>, Throws.Nothing);
        Assert.That(testNode.Format, Is.TypeOf<VabHeader>());
    }
}
