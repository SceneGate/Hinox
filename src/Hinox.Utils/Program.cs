string consoleVersion = typeof(Program).Assembly.GetName()?.Version?.ToString() ?? "0.0.1";
Console.WriteLine($"Console version: {consoleVersion}");
