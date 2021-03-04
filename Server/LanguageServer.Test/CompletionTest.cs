using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Server;
using Xunit;

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

        public static readonly DocumentUri DefaultURI = new (null, null, null, null, null, null);

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

            var document = new SCLDocument(text, DefaultURI);

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
}