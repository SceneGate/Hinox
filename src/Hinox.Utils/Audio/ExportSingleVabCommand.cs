namespace SceneGate.Hinox.Utils.Audio;

using System.ComponentModel;
using System.Text;
using Data.HashFunction;
using Data.HashFunction.CRC;
using SceneGate.Hinox.Audio;
using Spectre.Console;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using Yarhl.FileSystem;
using Yarhl.IO;

[Description("Extract the audios from a VAB or VH/VB format")]
internal class ExportSingleVabCommand : Command<ExportSingleVabCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--vab")]
        [Description("Path to the VAB file. Ignore if VH and VB paths are provided.")]
        public string? VabPath { get; set; }

        [CommandOption("--vh")]
        [Description("Path to the VH file (VAB header). Ignore if VAB path is provided.")]
        public string? HeaderPath { get; set; }

        [CommandOption("--vb")]
        [Description("Path to the VB file (VAB body). Ignore if VAB path is provided.")]
        public string? BodyPath { get; set; }

        [CommandOption("--files")]
        [Description("Optional path to a previously exported files.yml for having custom names when exporting")]
        public string? NamesPath { get; set; }

        [CommandOption("-o|--output")]
        [Description("Path to the directory to write the output files.")]
        public required string OutputPath { get; set; }

        public override ValidationResult Validate()
        {
            if (string.IsNullOrEmpty(OutputPath)) {
                return ValidationResult.Error("Output path is mandatory");
            }

            if (!string.IsNullOrEmpty(VabPath) && !string.IsNullOrEmpty(HeaderPath)) {
                return ValidationResult.Error("Specify either a VAB path or VH/VB paths, no both");
            }

            if (!string.IsNullOrEmpty(VabPath) && !File.Exists(VabPath)) {
                return ValidationResult.Error($"The input VAB file '{VabPath}' does NOT exists");
            }

            if (!string.IsNullOrEmpty(HeaderPath) && string.IsNullOrEmpty(BodyPath)) {
                return ValidationResult.Error($"A VB file is mandatory when providing a VH file");
            }

            if (!string.IsNullOrEmpty(HeaderPath) && !File.Exists(HeaderPath)) {
                return ValidationResult.Error($"The input VH file '{HeaderPath}' does NOT exists");
            }

            if (!string.IsNullOrEmpty(BodyPath) && string.IsNullOrEmpty(HeaderPath)) {
                return ValidationResult.Error($"A VH file is mandatory when providing a VB file");
            }

            if (!string.IsNullOrEmpty(BodyPath) && !File.Exists(BodyPath)) {
                return ValidationResult.Error($"The input VB file '{BodyPath}' does NOT exists");
            }

            if (!string.IsNullOrEmpty(NamesPath) && !File.Exists(NamesPath)) {
                return ValidationResult.Error($"The input names file '{NamesPath}' does NOT exists");
            }

            return base.Validate();
        }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        using Node container = ReadContainer(settings);
        AnsiConsole.MarkupLineInterpolated($"Found [blue]{container.Children.Count - 1}[/] audios");

        AnsiConsole.WriteLine($"Exporting content into folder: '{Path.GetFullPath(settings.OutputPath)}'");
        ExportHeader(container, settings.OutputPath);
        ExportAudios(container, settings.OutputPath, settings.NamesPath);
        return 0;
    }

    private static void ExportHeader(Node container, string outputPath)
    {
        VabHeader header = container.Children["header"]!.GetFormatAs<VabHeader>()!;
        string yaml = new SerializerBuilder()
            .WithAttributeOverride<VabHeader>(x => x.WaveformSizes, new YamlIgnoreAttribute())
            .Build()
            .Serialize(header);

        Directory.CreateDirectory(outputPath);
        string fileOutputPath = Path.Combine(outputPath, "vab.yml");
        File.WriteAllText(fileOutputPath, yaml, Encoding.UTF8);
    }

    private static void ExportAudios(Node container, string outputPath, string? namesPath)
    {
        ContainerInfo? inputNames = null;
        if (!string.IsNullOrEmpty(namesPath)) {
            AnsiConsole.WriteLine($"Reading provided file names from: '{Path.GetFullPath(namesPath)}'");
            inputNames = ContainerInfo.FromYaml(namesPath);
        }

        var audios = container.Children.Where(n => n.Name != "header").ToArray();
        if (inputNames is not null && inputNames.Files.Count != audios.Length) {
            AnsiConsole.MarkupLine(
                $"[bold red]ERROR[/]: Number of names [blue]{inputNames.Files.Count}[/] " +
                $"does [red]NOT[/] match audio count [blue]{audios.Length}[/]");
            return;
        }

        for (int i = 0; i < audios.Length; i++) {
            string audioName = inputNames != null ? inputNames.Files[i].Path : GetAudioName(i, audios[i]);
            if (audios.Any(n => n.Name == audioName)) {
                AnsiConsole.MarkupLineInterpolated($"[gray]Skipping audio with duplicated name #{i} ({audioName})[/]");
                continue;
            }

            audios[i].Name = audioName;

            string audioOutputPath = Path.Combine(outputPath, audioName);
            audios[i].Stream!.WriteTo(audioOutputPath);
        }

        if (inputNames is null) {
            string fileOutputPath = Path.Combine(outputPath, "files.yml");
            ContainerInfo.Create(audios).WriteAsYaml(fileOutputPath);
        }
    }

    private static string GetAudioName(int index, Node audio)
    {
        audio.Stream!.Position = 0;

        ICRC crc = CRCFactory.Instance.Create(CRCConfig.CRC32);
        IHashValue hash = crc.ComputeHash(audio.Stream);
        return $"{index:D4}_{hash.AsHexString().ToUpperInvariant()}.vag";
    }

    private static Node ReadContainer(Settings settings)
    {
        return string.IsNullOrEmpty(settings.VabPath)
            ? ReadHeaderBodyFiles(settings.HeaderPath!, settings.BodyPath!)
            : ReadVabFile(settings.VabPath);
    }

    private static Node ReadVabFile(string vabPath)
    {
        AnsiConsole.WriteLine($"Reading VAB '{Path.GetFullPath(vabPath)}'");
        return NodeFactory.FromFile(vabPath, FileOpenMode.Read)
            .TransformWith<BinaryVab2Container>();
    }

    private static Node ReadHeaderBodyFiles(string headerPath, string bodyPath)
    {
        AnsiConsole.WriteLine($"Reading VH '{Path.GetFullPath(headerPath)}");
        Node headerNode = NodeFactory.FromFile(headerPath, "header", FileOpenMode.Read)
            .TransformWith<Binary2VabHeader>();
        VabHeader header = headerNode.GetFormatAs<VabHeader>()!;

        AnsiConsole.WriteLine($"Reading VB '{Path.GetFullPath(bodyPath)}");
        Node container = NodeFactory.FromFile(bodyPath, FileOpenMode.Read)
            .TransformWith(new BinaryVabBody2Container(header));

        container.Add(headerNode);

        return container;
    }
}
