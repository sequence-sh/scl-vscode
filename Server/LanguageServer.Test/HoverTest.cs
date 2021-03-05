using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Xunit;
using Server;

namespace LanguageServer.Test
{
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

        public static readonly DocumentUri DefaultURI = new (null, null, null, null, null, null);

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

            var document = new SCLDocument(text, DefaultURI);

            var position = new Position(line, character);

            var hover = document.GetHover(position, sfs);

            if (string.IsNullOrWhiteSpace(expectedHover))
                hover.Contents.Should().BeNull();

            else
            {
                hover.Contents.Should().NotBeNull($"Should have Hover '{expectedHover}'");
                hover.Contents.HasMarkedStrings.Should().BeTrue($"Should have Hover '{expectedHover}'");
            }

            hover.Contents.MarkedStrings!.First().Value.Should().Be(expectedHover);
        }
    }
}