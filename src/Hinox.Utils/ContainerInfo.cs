namespace SceneGate.Hinox.Utils;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using YamlDotNet.Serialization;
using Yarhl.FileSystem;

internal record ContainerInfo
{
    public Collection<ExportedFileInfo> Files { get; init; } = [];

    public static ContainerInfo Create(IEnumerable<Node> nodes)
    {
        var infos = nodes
            .Where(n => n.Stream is not null)
            .Select(n => new ExportedFileInfo(n.Name, n.Stream!.Length))
            .ToList();
        return new ContainerInfo {
            Files = new Collection<ExportedFileInfo>(infos),
        };
    }

    public string ToYaml()
    {
        return new SerializerBuilder()
            .Build()
            .Serialize(this);
    }

    internal record ExportedFileInfo(string Path, long MaxLength);
}
