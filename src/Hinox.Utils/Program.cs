using SceneGate.Hinox.Utils.Audio;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(static configurator => {
    configurator.SetExceptionHandler((ex, _) => AnsiConsole.WriteException(ex));

    configurator.AddBranch("audio", static audio => {
        audio.SetDescription("Audio file formats");

        audio.AddBranch("vab", static vab => {
            vab.SetDescription("VAB and VH/VB audio format");
            vab.AddCommand<ExportSingleVabCommand>("export");
            vab.AddCommand<ImportSingleVabCommand>("import");
        });
    });
});

int result = await app.RunAsync(args);

string resultColor = result == 0 ? "green" : "red";
AnsiConsole.WriteLine();
AnsiConsole.MarkupLineInterpolated($"[bold {resultColor}]Done ({result})![/]");

return result;
