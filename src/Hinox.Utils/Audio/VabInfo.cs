namespace SceneGate.Hinox.Utils.Audio;

using System.Collections.ObjectModel;
using System.Linq;
using SceneGate.Hinox.Audio;
using YamlDotNet.Serialization;
using Yarhl.FileSystem;

internal record VabInfo
{
    public required VabHeader Header { get; init; }

    public Collection<AudioInfo> Audios { get; init; } = [];

    public static VabInfo CreateFromContainer(Node container, string audioExtension)
    {
        VabHeader header = container.Children["header"]!.GetFormatAs<VabHeader>()!;
        var audios = container.Children.Where(n => n.Name != "header");
        var audioInfos = audios.Select(n => new AudioInfo(n.Name + audioExtension, n.Stream!.Length));

        return new VabInfo {
            Header = header,
            Audios = new Collection<AudioInfo>(audioInfos.ToList()),
        };
    }

    public string ToYaml()
    {
        return new SerializerBuilder()
            .WithAttributeOverride<VabHeader>(x => x.WaveformSizes, new YamlIgnoreAttribute())
            .Build()
            .Serialize(this);
    }

    internal record AudioInfo(string Path, long MaxLength);
}
