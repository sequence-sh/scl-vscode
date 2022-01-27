using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.Sequence.Core.Internal;
using Xunit;
using static LanguageServer.Test.TestHelpers;

namespace LanguageServer.Test;

public class FormattingTest
{
    [Theory]
    [InlineData("Print 123", "Print Value: 123 [start: (0, 0), end: (0, 9)]")]
    [InlineData("- Print 123\r\n- a b\r\n- Print 456", "- Print Value: 123 [start: (0, 0), end: (0, 11)]", "- Print Value: 456 [start: (2, 0), end: (2, 11)]")]
    public void ShouldGiveCorrectFormatting(string text, params string[] expectedFormattings)
    {
        var sfs = StepFactoryStore.Create();

        var document = new SCLDocument(text, DefaultUri);

        var textEdits = document.FormatDocument(sfs);

        var actual = textEdits.Select(x => $"{x.NewText} {x.Range}").ToList();

        actual.Should().BeEquivalentTo(expectedFormattings);
    }

}

public class HoverTest
{

    [Theory]
    [InlineData("Print 123", 0, 1, "`Print`", "`Unit`", "Prints a value to the console.")]
    [InlineData("Print 123", 0, 8, "`123`",  "`SCLInt`")]
    [InlineData("- Print 123\r\n- a b", 0 ,4, "`Print`", "`Unit`", "Prints a value to the console." )]
    //[InlineData("- Print 123\r\n- a b", 1 ,1, "Syntax Error: no viable alternative at input '- a b'" )]
    [InlineData("- <val> = 123\r\n- print <val>", 1,9, "`<val>`", "`SCLInt`")]
    [InlineData(LongText, 0, 12, "`'Blake, Robert'`", "`StringStream`")]
    [InlineData(LongText, 1, 3, "`ArrayFilter`", "`Array of T`", "Filter an array or entity stream using a conditional statement")]
    [InlineData(LongText, 1, 14, "`Predicate`", "`T`", "A function that determines whether an entity should be included.")]
    public void ShouldGiveCorrectHover(string text, int line, int character, params string[] expectedHovers)
    {
        var sfs = StepFactoryStore.Create();

        var document = new SCLDocument(text, DefaultUri);

        var position = new Position(line, character);

        var hover = document.GetHover(position, sfs);

        if (!expectedHovers.Any())
            hover.Contents.Should().BeNull();

        else
        {
            hover.Contents.Should().NotBeNull($"Should have Hover");
            hover.Contents.HasMarkedStrings.Should().BeTrue($"Should have Hover");

            var actualHovers = hover.Contents.MarkedStrings!.Select(x => x.Value).ToList();

            actualHovers.Should().BeEquivalentTo(expectedHovers);
        }
    }
}