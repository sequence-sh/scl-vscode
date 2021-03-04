using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Xunit;
using Server;

namespace LanguageServer.Test
{
    public class CompletionTest
    {
        public const string LongText = @"FileRead    'artwork_data.csv'
| FromCSV    
| ArrayFilter ((from <entity> 'artist') == 'Blake, Robert')
| EntityMap (in <entity> 'artist' (StringToCase (from <entity> 'artist') TextCase.Upper ))
| EntityMapProperties (artist: 'Artist Name' artistId: 'ArtistId')
| ArraySort (from <entity> 'year')
| ArrayDistinct (from <entity> 'id')
| ToJson
| FileWrite 'Artwork_Data.json'
";

        public const string ErrorText = @"- FileRead 'artwork_data.csv'
- 0.1.2.3";

        [Theory]
        [InlineData("Print 123", 0, 1, "Print")]
        [InlineData("Print 123", 0, 10, "Print")]
        [InlineData(LongText, 0, 1, "FileRead")]
        [InlineData(LongText, 0, 9, "Path")]
        [InlineData(LongText, 1, 3,
            "FromCSV")]
        [InlineData(ErrorText, 0, 1, "FileRead")]
        public void ShouldGiveCorrectCompletion(string text, int line, int character, string? expectedLabel)
        {
            var sfs = StepFactoryStore.CreateUsingReflection(typeof(IStep));

            var document = new SCLDocument(text);

            var position = new Position(line, character);

            var completionList = document.GetCompletionList(position, sfs);


            if (string.IsNullOrWhiteSpace(expectedLabel))
                completionList.Should().BeEmpty();

            else
            {
                var labels =
                    completionList.Items.Select(x => x.Label);
                labels.Should().Contain(expectedLabel);
                foreach (var item in completionList.Items)
                {
                    item.Documentation.Should().NotBeNull();
                    item.Documentation!.HasMarkupContent.Should().BeTrue();
                    item.Documentation.MarkupContent!.Value.Should().NotBeEmpty();
                }
            }
        }
    }


    public class HoverTest
    {
        public const string LongText = @"FileRead 'artwork_data.csv'
| FromCSV
| ArrayFilter ((from <entity> 'artist') == 'Blake, Robert')
| EntityMap (in <entity> 'artist' (StringToCase (from <entity> 'artist') TextCase.Upper ))
| EntityMapProperties (artist: 'Artist Name' artistId: 'ArtistId')
| ArraySort (from <entity> 'year')
| ArrayDistinct (from <entity> 'id')
| ToJson
| FileWrite 'Artwork_Data.json'
";

        public const string ErrorText = @"- FileRead 'artwork_data.csv'
- 0.1.2.3";

        [Theory]
        [InlineData("Print 123", 0, 1, "Prints a value to the console.")]
        [InlineData("Print 123", 0, 8, "The Value to Print.")]
        [InlineData(LongText, 0, 1, "Reads text from a file.")]
        [InlineData(LongText, 0, 12, "The name of the file to read.")]
        [InlineData(LongText, 1, 3,
            "Extracts entities from a CSV file.\nThe same as FromConcordance but with different default values.")]
        [InlineData(ErrorText, 0, 1, "Reads text from a file.")]
        public void ShouldGiveCorrectHover(string text, int line, int character, string expectedHover)
        {
            var sfs = StepFactoryStore.CreateUsingReflection(typeof(IStep));

            var document = new SCLDocument(text);

            var position = new Position(line, character);

            var hover = document.GetHover(position, sfs);

            if (string.IsNullOrWhiteSpace(expectedHover))
                hover.Contents.Should().BeNull();

            else
            {
                hover.Contents.Should().NotBeNull($"Should have Hover '{expectedHover}'");
                hover.Contents.HasMarkedStrings.Should().BeTrue($"Should have Hover '{expectedHover}'");
            }

            hover.Contents.MarkedStrings.First().Value.Should().Be(expectedHover);
        }
    }
}