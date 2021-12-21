using System.Linq;
using FluentAssertions;
using Reductech.Sequence.Core.Internal;
using Xunit;

namespace LanguageServer.Test;

public class DiagnosticsTest
{
    [Theory]
    [InlineData("Print 123")]
    [InlineData("Pront 123","The step 'Pront' does not exist[start: (0, 0), end: (0, 8)]")]
    [InlineData("a b","Syntax Error: no viable alternative at input 'b'[start: (0, 2), end: (0, 3)]")]
    [InlineData("- print 1\r\n- a b","Syntax Error: no viable alternative at input '- a b'[start: (1, 0), end: (1, 5)]")]
    [InlineData("- print 1\r\n- a b\r\n- print 2","Syntax Error: no viable alternative at input '- a b'[start: (1, 0), end: (1, 5)]")]
    [InlineData("- print 1\r\n- a b\r\n- c d\r\n- print 3","Syntax Error: no viable alternative at input '- a b'[start: (1, 0), end: (1, 5)]", "Syntax Error: no viable alternative at input '- c d'[start: (2, 0), end: (2, 5)]")]
    public void TestGetDiagnostics(string text, params string[] expectedErrors)
    {
        var document = new SCLDocument(text, TestHelpers.DefaultUri);
        var sfs = StepFactoryStore.Create();

        var diagnostics = document.GetDiagnostics(sfs);

        diagnostics.Diagnostics.Select(x => x.Message + x.Range).Should().BeEquivalentTo(expectedErrors);
    }
}