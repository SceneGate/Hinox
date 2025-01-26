namespace SceneGate.Hinox.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using NUnit.Framework;

public static class TestDataBase
{
    public static string VabResources => Path.Combine(RootFromOutputPath, "VAB");

    public static string RootFromOutputPath {
        get {
            string envVar = Environment.GetEnvironmentVariable("SCENEGATE_TEST_DIR");
            if (!string.IsNullOrEmpty(envVar)) {
                return envVar;
            }

            string programDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(
                programDir,
                "..", // framework
                "..", // configuration
                "..", // bin
                "Resources");
            return Path.GetFullPath(path);
        }
    }

    public static void IgnoreIfFileDoesNotExist(string file)
    {
        if (!File.Exists(file)) {
            string msg = $"[{TestContext.CurrentContext.Test.ClassName}] Missing resource file: {file}";
            TestContext.Progress.WriteLine(msg);
            Assert.Ignore(msg);
        }
    }

    public static IEnumerable<string> ReadListFile(string testDir, string fileName)
    {
        string filePath = Path.Combine(testDir, fileName);
        if (!File.Exists(filePath)) {
            return [];
        }

        return File.ReadAllLines(filePath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'));
    }

    public static IEnumerable<TestCaseData> ReadTestCaseDataListFile(string testDir, string fileName)
    {
        return ReadListFile(testDir, fileName)
            .Select(data => data.Split(','))
            .Select(data => new TestCaseData(data));
    }

    public static IEnumerable<string> ReadGlobFile(string testDir, string fileName)
    {
        string filePath = Path.Combine(testDir, fileName);
        if (!File.Exists(filePath)) {
            return [];
        }

        IEnumerable<string> patterns = ReadListFile(testDir, fileName);
        IEnumerable<string> includePatterns = patterns.Where(l => l[0] != '!');
        IEnumerable<string> excludePatterns = patterns.Where(l => l[0] == '!').Select(l => l[1..]);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(includePatterns);
        matcher.AddExcludePatterns(excludePatterns);

        return matcher.GetResultsInFullPath(testDir);
    }

    public static IEnumerable<TestCaseData> ReadTestCaseDataGlobFile(string testDir, string fileName)
    {
        return ReadGlobFile(testDir, fileName)
            .Select(p => new TestCaseData(p)
                .SetArgDisplayNames(Path.GetRelativePath(RootFromOutputPath, p)));
    }
}
