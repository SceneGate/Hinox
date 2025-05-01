namespace SceneGate.Hinox.Tests.Audio;

using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SceneGate.Hinox.Audio;
using SceneGate.Hinox.Tests.Framework;
using VerifyNUnit;
using Yarhl.FileSystem;
using Yarhl.IO;

[TestFixture]
public class BinaryVabBody2ContainerTests
{
    private static IEnumerable VbSnapshotTestFiles =>
        TestDataBase.ReadTestCaseDataListFile(TestDataBase.VabResources, "vb_snapshots.txt");

    [TestCaseSource(nameof(VbSnapshotTestFiles))]
    public Task VerifyUnpackForSnapshots(string vbName, string vhName)
    {
        string vbPath = Path.Combine(TestDataBase.VabResources, "Snapshots", vbName);
        string vhPath = Path.Combine(TestDataBase.VabResources, "Snapshots", vhName);
        TestDataBase.IgnoreIfFileDoesNotExist(vbPath);
        TestDataBase.IgnoreIfFileDoesNotExist(vhPath);

        using var vhBinary = new BinaryFormat(vhPath, FileOpenMode.Read);
        var header = new Binary2VabHeader().Convert(vhBinary);

        using var vbBinary = new BinaryFormat(vbPath, FileOpenMode.Read);
        var actual = new BinaryVabBody2Container(header).Convert(vbBinary);

        Verifier.UseProjectRelativeDirectory("Resources/VAB/Snapshots");
        return Verifier.Verify(actual.Root.Children)
            .UseFileName(vbName)
            .AddExtraSettings(x => {
                x.DefaultValueHandling = Argon.DefaultValueHandling.Include;
                x.Converters.Add(new BinaryVerifierJsonConverter());
            })
            .IgnoreMembers<NavigableNode<Node>>(x => x.Disposed, x => x.Path, x => x.Parent)
            .IgnoreMembers<Node>(x => x.IsContainer);
    }
}
