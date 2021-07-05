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
        [InlineData("Print 123", 0, 1, "`Print`", "`Unit`", "Prints a value to the console.")]
        [InlineData("Print 123", 0, 8, "`123`",  "`Integer`")]
        //[InlineData(LongText, 0, 1, "Reads text from a file.")] //doesn't work
        [InlineData(LongText, 0, 12, "`'artwork_data.csv'`", "`String`")]
        [InlineData(LongText, 1, 3,
            "`FromCSV`", "`Array<T>`", "Extracts entities from a CSV file.\nThe same as FromConcordance but with different default values.")]
        //[InlineData(ErrorText, 0, 1, "Reads text from a file.")]
        public void ShouldGiveCorrectHover(string text, int line, int character, params string[] expectedHovers)
        {
            var fsAssembly = Assembly.GetAssembly(typeof(FileRead))!;
            var sdAssembly = Assembly.GetAssembly(typeof(ToJson))!;

            var fsConnectorData = new ConnectorData(ConnectorSettings.DefaultForAssembly(fsAssembly), fsAssembly);
            var sdConnectorData = new ConnectorData(ConnectorSettings.DefaultForAssembly(sdAssembly), sdAssembly);

            var sfs = StepFactoryStore.Create(fsConnectorData, sdConnectorData);

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