using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Xunit;
using static LanguageServer.Test.TestHelpers;

namespace LanguageServer.Test
{
    public class HoverTest
    {

        public const string ErrorText = @"- FileRead 'artwork_data.csv'
- 0.1.2.3";

        [Theory]
        [InlineData("Print 123", 0, 1, "`Print`", "`Unit`", "Prints a value to the console.")]
        [InlineData("Print 123", 0, 8, "`123`",  "`Integer`")]
        //[InlineData(LongText, 0, 1, "Reads text from a file.")] //doesn't work
        [InlineData(LongText, 0, 12, "`'Blake, Robert'`", "`String`")]
        [InlineData(LongText, 1, 3, "`ArrayFilter`", "`Array<T>`", "Filter an array according to a function.")]
        //[InlineData(ErrorText, 0, 1, "Reads text from a file.")]
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
}