﻿namespace SceneGate.Hinox.Utils;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

    public static ContainerInfo FromYaml(string inputPath)
    {
        string yaml = File.ReadAllText(inputPath, Encoding.UTF8);
        return new DeserializerBuilder()
            .Build()
            .Deserialize<ContainerInfo>(yaml);
    }

    public void WriteAsYaml(string outputPath)
    {
        string yaml = new SerializerBuilder()
            .Build()
            .Serialize(this);

        File.WriteAllText(outputPath, yaml, Encoding.UTF8);
    }

    internal record ExportedFileInfo(string Path, long MaxLength)
    {
        public ExportedFileInfo()
            : this(string.Empty, -1)
        {
        }
    }
}
