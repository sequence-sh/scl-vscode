using System.Linq;
using FluentAssertions;
using Sequence.Core.Internal;
using Xunit;

namespace LanguageServer.Test;

public class DiagnosticsTest
{
    [Theory]
    [InlineData("Print 123")]
    [InlineData("Pront 123","The step 'Pront' does not exist[start: (0, 0), end: (0, 8)]")]
    [InlineData("a b","The step 'a' does not exist[start: (0, 0), end: (0, 2)]")]
    [InlineData("- print 1\r\n- a b","The step 'a' does not exist[start: (1, 2), end: (1, 4)]")]
    [InlineData("- print 1\r\n- a b\r\n- print 2","The step 'a' does not exist[start: (1, 2), end: (1, 4)]")]
    [InlineData("- print 1\r\n- a b\r\n- c d\r\n- print 3",
        "The step 'a' does not exist[start: (1, 2), end: (1, 4)]",
        "The step 'c' does not exist[start: (2, 2), end: (2, 4)]")]
    public void TestGetDiagnostics(string text, params string[] expectedErrors)
    {
        var document = new SCLDocument(text, TestHelpers.DefaultUri);
        var sfs = StepFactoryStore.Create();

        var diagnostics = document.GetDiagnostics(sfs);

        diagnostics.Diagnostics.Select(x => x.Message + x.Range).Should().BeEquivalentTo(expectedErrors);
    }
}