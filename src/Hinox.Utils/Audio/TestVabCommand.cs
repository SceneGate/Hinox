namespace SceneGate.Hinox.Utils.Audio;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using SceneGate.Hinox.Audio;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using Yarhl.FileSystem;
using Yarhl.IO;

[Description("Test reading and writing VAB files")]
internal sealed class TestVabCommand : Command<TestVabCommand.Settings>
{
    private ILogger<TestVabCommand> logger = null!;

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[PATH]")]
        [Description("Path to the directory containing VAB or VH/VB files")]
        public required string InputPath { get; set; }

        [CommandOption("-r|--recursive")]
        [Description("Indicates whether to search in subdirectories recursively")]
        public bool RecursiveSearch { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        logger = AppLoggerFactory.CreateLogger<TestVabCommand>();

        var vabResults = VabSearchAndTest(settings);
        var vhvbResults = VhVbSearchAndTest(settings);

        var combinedResults = vabResults.Concat(vhvbResults);
        PrintResult(combinedResults);

        return 0;
    }

    private static ReadOnlyCollection<FormatTestResult> VabSearchAndTest(Settings settings)
    {
        List<FormatTestResult> results = [];

        var searchOptions = settings.RecursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var matchingFiles = Directory.EnumerateFiles(settings.InputPath, "*.vab", searchOptions).ToArray();

        AnsiConsole.Progress().Start(x => {
            var task = x.AddTask("Analyzing VAB files", maxValue: matchingFiles.Length);

            for (int i = 0; i < matchingFiles.Length; i++) {
                var result = TestVab(matchingFiles[i]);
                results.Add(result);
                task.Increment(1);
            }
        });

        return results.AsReadOnly();
    }

    private ReadOnlyCollection<FormatTestResult> VhVbSearchAndTest(Settings settings)
    {
        List<FormatTestResult> results = [];

        var searchOptions = settings.RecursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var matchingFiles = Directory.EnumerateFiles(settings.InputPath, "*.vh", searchOptions).ToArray();

        AnsiConsole.Progress().Start(x => {
            var task = x.AddTask("Analyzing VH/VB files", maxValue: matchingFiles.Length);

            foreach (string vhPath in matchingFiles) {
                string vbPath = Path.ChangeExtension(vhPath, ".vb");
                if (!File.Exists(vbPath)) {
                    logger.LogDebug("VH file without matching VB {Path}", vhPath);
                    task.Increment(1);
                    continue;
                }

                var result = TestVhVb(vhPath, vbPath);
                results.AddRange(result);
                task.Increment(1);
            }
        });

        return results.AsReadOnly();
    }

    private static FormatTestResult TestVab(string inputVab)
    {
        using var binaryVab = new BinaryFormat(inputVab, FileOpenMode.Read);

        NodeContainerFormat container;
        try {
            container = new BinaryVab2Container(includePaddingTones: true, throwOnInvalid: false)
                .Convert(binaryVab);
        } catch (Exception ex) {
            return new FormatTestResult(inputVab, "VAB", false, null, null, ex.Message);
        }

        BinaryFormat generatedVab;
        try {
            generatedVab = new Container2BinaryVab(autodetectVag: false).Convert(container);
        } catch (Exception ex) {
            container.Dispose();
            return new FormatTestResult(inputVab, "VAB", true, false, null, ex.Message);
        }

        bool isIdentical = generatedVab.Stream.Compare(binaryVab.Stream);
        generatedVab.Dispose();

        return new FormatTestResult(inputVab, "VAB", true, true, isIdentical, null);
    }

    private static IEnumerable<FormatTestResult> TestVhVb(string inputVh, string inputVb)
    {
        using var binaryVh = new BinaryFormat(inputVh, FileOpenMode.Read);
        using var binaryVb = new BinaryFormat(inputVb, FileOpenMode.Read);

        VabHeader header;
        try {
            header = new Binary2VabHeader(includePaddingTones: true, throwOnInvalid: false)
                .Convert(binaryVh);
        } catch (Exception ex) {
            return [new FormatTestResult(inputVh, "VH", false, null, null, ex.Message)];
        }

        NodeContainerFormat container;
        try {
            container = new BinaryVabBody2Container(header).Convert(binaryVb);
        } catch (Exception ex) {
            return [new FormatTestResult(inputVb, "VB", false, null, null, ex.Message)];
        }

        BinaryFormat generatedVh;
        try {
            generatedVh = new VabHeader2Binary().Convert(header);
        } catch (Exception ex) {
            container.Dispose();
            return [new FormatTestResult(inputVh, "VH", true, false, null, ex.Message)];
        }

        BinaryFormat generatedVb;
        try {
            generatedVb = new Container2BinaryVabBody(autodetectVag: false).Convert(container);
        } catch (Exception ex) {
            generatedVh.Dispose();
            container.Dispose();
            return [new FormatTestResult(inputVb, "VB", true, false, null, ex.Message)];
        }

        bool isVhIdentical = generatedVh.Stream.Compare(binaryVh.Stream);
        generatedVh.Dispose();

        bool isVbIdentical = generatedVb.Stream.Compare(binaryVb.Stream);
        generatedVb.Dispose();

        return [
            new FormatTestResult(inputVh, "VH", true, true, isVhIdentical, null),
            new FormatTestResult(inputVb, "VB", true, true, isVbIdentical, null),
        ];
    }

    private static void PrintResult(IEnumerable<FormatTestResult> results)
    {
        static IRenderable RenderIsSucceed(bool? x) =>
            x switch {
                null => new Text(string.Empty),
                true => new Text("Success"),
                false => new Markup("[red]Failure[/]"),
            };

        AnsiConsole.Write(new Rule("Results"));

        var table = new Table();
        table.AddColumns("Path", "Format", "Read", "Write", "Identical", "Error");

        var readResults = new TestTotalResult();
        var writeResults = new TestTotalResult();
        var identicalResults = new TestTotalResult();
        foreach (var result in results) {
            readResults.AddResult(result.ReadSucceed);
            writeResults.AddResult(result.WriteSucceed);
            identicalResults.AddResult(result.Identical);

            table.AddRow(
                new TextPath(result.Path),
                new Text(result.Format),
                RenderIsSucceed(result.ReadSucceed),
                RenderIsSucceed(result.WriteSucceed),
                RenderIsSucceed(result.Identical),
                new Text(result.ErrorMessage ?? string.Empty));
        }

        AnsiConsole.Write(table);

        var totalTable = new Table();
        totalTable.AddColumns("Operation", "Total", "Successful", "Failed", "Indeterminate");
        totalTable.AddRow(
            "Read",
            readResults.Total.ToString(),
            readResults.GetSuccessfulRate(),
            readResults.GetFailedRate(),
            readResults.Indeterminate.ToString());
        totalTable.AddRow(
            "Write",
            writeResults.Total.ToString(),
            writeResults.GetSuccessfulRate(),
            writeResults.GetFailedRate(),
            writeResults.Indeterminate.ToString());
        totalTable.AddRow(
            "Identical",
            identicalResults.Total.ToString(),
            identicalResults.GetSuccessfulRate(),
            identicalResults.GetFailedRate(),
            identicalResults.Indeterminate.ToString());
        AnsiConsole.Write(totalTable);
    }

    private sealed record FormatTestResult(
        string Path,
        string Format,
        bool ReadSucceed,
        bool? WriteSucceed,
        bool? Identical,
        string? ErrorMessage);

    private sealed class TestTotalResult
    {
        public int Total { get; private set; }

        public int Successful {  get; private set; }

        public int Failed { get; private set; }

        public int Indeterminate { get; private set; }

        public string GetSuccessfulRate() => $"{Successful} ({Successful / (Total - Indeterminate):P1})";

        public string GetFailedRate() => $"{Failed} ({Failed / (Total - Indeterminate):P1})";

        public void AddResult(bool? result)
        {
            Total++;
            if (result.HasValue) {
                if (result.Value) {
                    Successful++;
                } else {
                    Failed++;
                }
            } else {
                Indeterminate++;
            }
        }
    }
}
