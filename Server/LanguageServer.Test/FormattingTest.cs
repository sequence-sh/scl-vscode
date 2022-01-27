using System.Linq;
using FluentAssertions;
using Reductech.Sequence.Core.Internal;
using Xunit;

namespace LanguageServer.Test;

public class FormattingTest
{
    [Theory]
    [InlineData("Print 123", "Print Value: 123 [start: (0, 0), end: (0, 9)]")]
    [InlineData("- Print 123\r\n- a b\r\n- Print 456", "- Print Value: 123 [start: (0, 0), end: (0, 11)]", "- Print Value: 456 [start: (2, 0), end: (2, 11)]")]
    public void ShouldGiveCorrectFormatting(string text, params string[] expectedFormattings)
    {
        var sfs = StepFactoryStore.Create();

        var document = new SCLDocument(text, TestHelpers.DefaultUri);

        var textEdits = document.FormatDocument(sfs);

        var actual = textEdits.Select(x => $"{x.NewText} {x.Range}").ToList();

        actual.Should().BeEquivalentTo(expectedFormattings);
    }

}