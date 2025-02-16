namespace SceneGate.Hinox.Utils.Audio;

using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using SceneGate.Hinox.Audio;
using Spectre.Console;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using Yarhl.FileSystem;
using Yarhl.IO;

[Description("Import audio files into VAB or VH/VB format")]
internal class ImportSingleVabCommand : Command<ImportSingleVabCommand.Settings>
{
    private ILogger<ImportSingleVabCommand> logger = null!;

    public sealed class Settings : CommandSettings
    {
        [CommandOption("-f|--files")]
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

        [CommandOption("--autodetect-vag")]
        [Description("If set, it tries to autodetect VAG headers in audio files")]
        [DefaultValue(false)]
        public bool AutoDetectVag { get; set; }

        [CommandOption("-v|--verbosity")]
        [Description("Logging output verbosity")]
        [DefaultValue(LogLevel.Warning)]
        public LogLevel Verbosity { get; set; }

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
        AppLoggerFactory.MinimumLevel = settings.Verbosity;
        logger = AppLoggerFactory.CreateLogger<ImportSingleVabCommand>();

        using Node? container = ReadContainer(settings.ContainerInfoPath, settings.VabHeader, settings.AutoDetectVag);
        if (container is null) {
            return 1;
        }

        if (!string.IsNullOrEmpty(settings.OutputVabPath)) {
            ExportVab(container, settings.AutoDetectVag, settings.OutputVabPath);
        } else {
            ExportVhVb(container, settings.AutoDetectVag, settings.OutputHeaderPath!, settings.OutputBodyPath!);
        }

        return 0;
    }

    private void ExportVab(Node container, bool autodetect, string outputPath)
    {
        logger.LogInformation("Exporting as VAB format into '{Path}'", Path.GetFullPath(outputPath));
        container.TransformWith(new Container2BinaryVab(autodetect))
            .Stream!.WriteTo(outputPath);
    }

    private void ExportVhVb(Node container, bool autodetect, string outVhPath, string outVbPath)
    {
        VabHeader header = container.Children["header"]!.GetFormatAs<VabHeader>()!;

        var audios = new NodeContainerFormat();
        audios.Root.Add(container.Children.Where(n => n.Name != "header"));

        Container2BinaryVab.UpdateFileSizes(header, audios,autodetect);

        logger.LogInformation("Exporting header in VH format into '{Path}'", Path.GetFullPath(outVhPath));
        using BinaryFormat binaryHeader = new VabHeader2Binary().Convert(header);
        binaryHeader.Stream!.WriteTo(outVhPath);

        logger.LogInformation("Exporting body in VB format into '{Path}'", Path.GetFullPath(outVbPath));
        using BinaryFormat binaryBody = new Container2BinaryVabBody(autodetect).Convert(audios);
        binaryBody.Stream.WriteTo(outVbPath);
    }

    private Node? ReadContainer(string infoPath, string headerPath, bool autodetect)
    {
        var container = NodeFactory.CreateContainer("vab");
        if (!AddHeader(headerPath, container)) {
            return null;
        }

        if (!AddAudios(infoPath, autodetect, container)) {
            return null;
        }

        return container;
    }

    private bool AddHeader(string headerPath, Node container)
    {
        logger.LogInformation("Reading header from YML '{Path}'", Path.GetFullPath(headerPath));
        try {
            string yaml = File.ReadAllText(headerPath, Encoding.UTF8);
            VabHeader header = new DeserializerBuilder()
                .Build()
                .Deserialize<VabHeader>(yaml);

            var headerNode = new Node("header", header);
            container.Add(headerNode);
            return true;
        } catch (Exception ex) {
            logger.LogError(ex, "Failed to read header YAML file");
            return false;
        }
    }

    private bool AddAudios(string infoPath, bool autodetect, Node container)
    {
        logger.LogInformation("Reading file info from YML '{Path}'", Path.GetFullPath(infoPath));
        string basePath = Path.GetDirectoryName(Path.GetFullPath(infoPath))!;

        ContainerInfo containerInfo;
        try {
            containerInfo = ContainerInfo.FromYaml(infoPath);
        }  catch (Exception ex) {
            logger.LogError(ex, "Failed to read info YAML file");
            return false;
        }

        if (containerInfo.Files.Count > VabHeader.MaximumWaveforms) {
            logger.LogError(
                "Maximum audio files is {Max} but info file contains {Actual}",
                VabHeader.MaximumWaveforms,
                containerInfo.Files.Count);
            return false;
        }

        logger.LogDebug("Found {Count} files", containerInfo.Files.Count);

        long totalLength = 0;
        for (int i = 0; i < containerInfo.Files.Count; i++) {
            Node audioNode = OpenAudio(i, containerInfo.Files[i], basePath, autodetect);
            container.Add(audioNode);

            totalLength += audioNode.Stream!.Length;
        }

        if (totalLength > VabHeader.MaximumTotalWaveformsSize) {
            logger.LogError(
                "Total audio length {Actual} is larger than supported by the format {Max}",
                totalLength,
                VabHeader.MaximumTotalWaveformsSize);
            return false;
        }

        return true;
    }

    private Node OpenAudio(int idx, ExportedFileInfo audioInfo, string basePath, bool autodetect)
    {
        string audioPath = Path.GetFullPath(audioInfo.Path, basePath);
        if (!File.Exists(audioPath)) {
            logger.LogError("Cannot find audio: '{Path}'", audioPath);
            throw new FileNotFoundException("Audio file not found", audioPath);
        }

        long audioOffset = audioInfo.Offset;
        if (audioOffset == 0 && autodetect) {
            using var tempVagStream = DataStreamFactory.FromFile(audioPath, FileOpenMode.Read);
            long audioLength = VagFormatAnalyzer.GetChannelsLength(tempVagStream);

            audioOffset = tempVagStream.Length - audioLength;
            if (audioOffset > 0) {
                logger.LogDebug("'{Name}' detected as VAG with header", audioInfo.Path);
            }
        }

        using var fullData = DataStreamFactory.FromFile(audioPath, FileOpenMode.Read); // temp stream for slicing
        var actualData = new BinaryFormat(fullData.Slice(audioOffset));

        // Rename to ensure duplicated files are added as copies instead of replaced
        var audioNode = new Node($"audio_{idx}", actualData);

        if (audioInfo.OriginalLength > -1 && audioNode.Stream!.Length > audioInfo.OriginalLength) {
            logger.LogWarning(
                "Audio '{Path}' with file size {Actual} larger than original size {Original}",
                audioInfo.Path,
                audioNode.Stream!.Length,
                audioInfo.OriginalLength);
        }

        return audioNode;
    }
}
