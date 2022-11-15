using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Sequence.Core.Internal;
using Xunit;
using static LanguageServer.Test.TestHelpers;

namespace LanguageServer.Test;

public class CompletionTest
{
        

    public const string ErrorText = @"- FileRead 'artwork_data.csv'
- 0.1.2.3";

    [Theory]
    [InlineData("Print 123", 0, 6, "Value")]
    [InlineData("- Print  ", 0, 11, "Value")]
    [InlineData("Print  ", 0, 9, "Value")]
    [InlineData("Print P", 0, 8, "Value")]
    [InlineData("Print\r\nP", 1, 0, "Value")]
    [InlineData("- Print\r\nP", 1, 0, "Value")]
    [InlineData("- Print P", 0, 8, "Value")]
    [InlineData(LongText, 1, 3, "ArrayFilter")]
    public void ShouldGiveCorrectCompletion(string text, int line, int character, string? expectedLabel)
    {

        var sfs = StepFactoryStore.Create();
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