namespace SceneGate.Hinox.Tests;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyNUnit;

[TestFixture]
public class VerifyConventionsCheck
{
    [Test]
    public Task Run() => VerifyChecks.Run();
}
