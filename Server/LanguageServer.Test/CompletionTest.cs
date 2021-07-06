using System.Linq;
using System.Reflection;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Connectors.FileSystem;
using Reductech.EDR.Connectors.StructuredData;
using Reductech.EDR.Core.Internal;
using Xunit;
using static LanguageServer.Test.TestHelpers;

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
        [InlineData(LongText, 0, 9, "Path")]
        [InlineData("- FileRead  ", 0, 11, "Path")]
        [InlineData("FileRead  ", 0, 9, "Path")]
        [InlineData("FileRead P", 0, 10, "Path")]
        [InlineData("- FileRead P", 0, 12, "Path")]
        [InlineData(LongText, 1, 3,
            "FromCSV")]
        public void ShouldGiveCorrectCompletion(string text, int line, int character, string? expectedLabel)
        {
            var fsAssembly = Assembly.GetAssembly(typeof(FileRead))!;
            var sdAssembly = Assembly.GetAssembly(typeof(ToJson))!;

            var fsConnectorData = new ConnectorData(ConnectorSettings.DefaultForAssembly(fsAssembly), fsAssembly);
            var sdConnectorData = new ConnectorData(ConnectorSettings.DefaultForAssembly(sdAssembly), sdAssembly);

            var sfs = StepFactoryStore.Create(fsConnectorData, sdConnectorData);
            var document = new SCLDocument(text, DefaultUri);

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