namespace SceneGate.Hinox.Utils.Audio;

using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SceneGate.Hinox.Audio;
using Spectre.Console;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using Yarhl.FileSystem;
using Yarhl.IO;

[Description("Import audio files into VAB or VH/VB format")]
internal class ImportSingleVabCommand : Command<ImportSingleVabCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-i|--info")]
        [Description("Path to the files.yml with the audio paths to import")]
        public required string ContainerInfoPath { get; set; }

        [CommandOption("--header")]
        [Description("Path to the vab.yml with the VAB header information")]
        public required string VabHeader { get; set; }

        [CommandOption("--out-vab")]
        [Description("Export content as VAB format into the provided path")]
        public string? OutputVabPath { get; set; }

        [CommandOption("--out-vh")]
        [Description("Export the header as VH format into the provided path")]
        public string? OutputHeaderPath { get; set; }

        [CommandOption("--out-vb")]
        [Description("Export the body container as VB format into the provided path")]
        public string? OutputBodyPath { get; set; }

        public override ValidationResult Validate()
        {
            if (!File.Exists(ContainerInfoPath)) {
                return ValidationResult.Error("Container info path is mandatory and must exists");
            }

            if (!File.Exists(VabHeader)) {
                return ValidationResult.Error("VAB header path is mandatory and must exists");
            }

            if (!string.IsNullOrEmpty(OutputVabPath) && !string.IsNullOrEmpty(OutputHeaderPath)) {
                return ValidationResult.Error("Specify either a VAB or VH/VB outputs, no both");
            }

            if (!string.IsNullOrEmpty(OutputHeaderPath) && string.IsNullOrEmpty(OutputBodyPath)) {
                return ValidationResult.Error($"A VB output is mandatory when providing a VH output");
            }

            if (!string.IsNullOrEmpty(OutputBodyPath) && string.IsNullOrEmpty(OutputHeaderPath)) {
                return ValidationResult.Error($"A VH output is mandatory when providing a VB output");
            }

            return base.Validate();
        }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        using Node container = ReadContainer(settings.ContainerInfoPath, settings.VabHeader);

        if (!string.IsNullOrEmpty(settings.OutputVabPath)) {
            ExportVab(container, settings.OutputVabPath);
        } else {
            ExportVhVb(container, settings.OutputHeaderPath!, settings.OutputBodyPath!);
        }

        return 0;
    }

    private static void ExportVab(Node container, string outputPath)
    {
        AnsiConsole.WriteLine($"Exporting as VAB format into '{Path.GetFullPath(outputPath)}'");
        container.TransformWith<Container2BinaryVab>()
            .Stream!.WriteTo(outputPath);
    }

    private static void ExportVhVb(Node container, string outVhPath, string outVbPath)
    {
        VabHeader header = container.Children["header"]!.GetFormatAs<VabHeader>()!;

        var audios = new NodeContainerFormat();
        audios.Root.Add(container.Children.Where(n => n.Name != "header"));

        Container2BinaryVab.UpdateFileSizes(header, audios);

        AnsiConsole.WriteLine($"Exporting header in VH format into '{Path.GetFullPath(outVhPath)}'");
        using BinaryFormat binaryHeader = new VabHeader2Binary().Convert(header);
        binaryHeader.Stream!.WriteTo(outVhPath);

        AnsiConsole.WriteLine($"Exporting body in VB format into '{Path.GetFullPath(outVbPath)}'");
        using BinaryFormat binaryBody = new Container2BinaryVabBody().Convert(audios);
        binaryBody.Stream.WriteTo(outVbPath);
    }

    private static Node ReadContainer(string infoPath, string headerPath)
    {
        var container = NodeFactory.CreateContainer("vab");
        AddHeader(headerPath, container);
        AddAudios(infoPath, container);

        AnsiConsole.MarkupLineInterpolated($"VAB container with [blue]{container.Children.Count}[/] nodes in total");
        return container;
    }

    private static void AddHeader(string headerPath, Node container)
    {
        AnsiConsole.WriteLine($"Reading header from YML '{Path.GetFullPath(headerPath)}'");
        string yaml = File.ReadAllText(headerPath, Encoding.UTF8);
        VabHeader header = new DeserializerBuilder()
            .Build()
            .Deserialize<VabHeader>(yaml);

        var headerNode = new Node("header", header);
        container.Add(headerNode);
    }

    private static void AddAudios(string infoPath, Node container)
    {
        AnsiConsole.WriteLine($"Reading file info from YML '{Path.GetFullPath(infoPath)}'");
        string basePath = Path.GetDirectoryName(Path.GetFullPath(infoPath))!;

        var containerInfo = ContainerInfo.FromYaml(infoPath);
        AnsiConsole.MarkupLineInterpolated($"Found [blue]{containerInfo.Files.Count}[/] files");

        for (int i = 0; i < containerInfo.Files.Count; i++) {
            var audioInfo = containerInfo.Files[i];
            string audioPath = Path.GetFullPath(audioInfo.Path, basePath);
            if (!File.Exists(audioPath)) {
                AnsiConsole.MarkupLineInterpolated($"[bold red]ERROR:[/] Cannot find audio: '{audioPath}'");
                throw new FileNotFoundException("Audio file not found", audioPath);
            }

            // Rename to ensure duplicated files are added as copies instead of replaced
            Node audioNode = NodeFactory.FromFile(audioPath, $"audio_{i}", FileOpenMode.Read);

            if (audioInfo.MaxLength > -1 && audioNode.Stream!.Length > audioInfo.MaxLength) {
                AnsiConsole.MarkupLine(
                    $"[bold red]ERROR:[/] Audio '{audioInfo.Path}' " +
                    $"with file size [red]{audioNode.Stream!.Length}[/] larger " +
                    $"than allowed [blue]{audioInfo.MaxLength}[/]");
                throw new InvalidOperationException("File larger than allowed");
            }

            container.Add(audioNode);
        }
    }
}
