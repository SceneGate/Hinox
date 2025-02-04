namespace SceneGate.Hinox.Utils.Audio;

using System.ComponentModel;
using System.Text;
using Microsoft.VisualBasic;
using SceneGate.Hinox.Audio;
using Spectre.Console;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using Yarhl.FileSystem;
using Yarhl.IO;

[Description("Extract the audios from a VAB or VH/VB audio container")]
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

            return base.Validate();
        }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        using Node container = ReadContainer(settings);
        AnsiConsole.MarkupLineInterpolated($"Found [blue]{container.Children.Count - 1}[/] audios");

        AnsiConsole.WriteLine($"Exporting content into folder: '{Path.GetFullPath(settings.OutputPath)}'");
        ExportHeader(container, settings.OutputPath);
        ExportAudios(container, settings.OutputPath);
        return 0;
    }

    private static void ExportHeader(Node container, string outputPath)
    {
        string yaml = VabInfo.CreateFromContainer(container, ".adpcm")
            .ToYaml();

        Directory.CreateDirectory(outputPath);
        string fileOutputPath = Path.Combine(outputPath, "vab.yml");
        File.WriteAllText(fileOutputPath, yaml, Encoding.UTF8);
    }

    private static void ExportAudios(Node container, string outputPath)
    {
        foreach (Node audio in container.Children.Where(n => n.Name != "header")) {
            string audioOutputPath = Path.Combine(outputPath, audio.Name + ".adpcm");
            audio.Stream!.WriteTo(audioOutputPath);
        }
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
