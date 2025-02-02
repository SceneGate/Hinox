namespace SceneGate.Hinox.Tests.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;
using SceneGate.Hinox.Audio;
using SceneGate.Hinox.Tests.Framework;
using VerifyNUnit;
using Yarhl.FileSystem;
using Yarhl.IO;

[TestFixture]
public class BinaryVab2ContainerTests
{
    private static IEnumerable VabSnapshotTestFiles =>
        TestDataBase.ReadTestCaseDataListFile(TestDataBase.VabResources, "vab_snapshots.txt");

        [TestCaseSource(nameof(VabSnapshotTestFiles))]
    public Task VerifyUnpackForSnapshots(string vabName)
    {
        string vabPath = Path.Combine(TestDataBase.VabResources, "Snapshots", vabName);
        TestDataBase.IgnoreIfFileDoesNotExist(vabPath);

        using var vabBinary = new BinaryFormat(vabPath, FileOpenMode.Read);
        var actual = new BinaryVab2Container().Convert(vabBinary);

        Verifier.UseProjectRelativeDirectory("Resources/VAB/Snapshots");
        return Verifier.Verify(actual.Root.Children)
            .UseFileName(vabName)
            .AddExtraSettings(x => {
                x.DefaultValueHandling = Argon.DefaultValueHandling.Include;
                x.Converters.Add(new BinaryVerifierJsonConverter());
                x.Converters.Add(new HexadecimalVerifierJsonConverter<VabHeader>());
            })
            .IgnoreMembers<NavigableNode<Node>>(x => x.Disposed, x => x.Path, x => x.Parent)
            .IgnoreMembers<Node>(x => x.IsContainer);
    }
}
