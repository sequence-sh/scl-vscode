using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.Sequence.Core.Internal;
using Xunit;
using static LanguageServer.Test.TestHelpers;

namespace LanguageServer.Test;

public class SignatureHelpTest
{
        

    [Theory]
    [InlineData("ArrayFilter", 0, 12, "ArrayFilter")]
    [InlineData("ArrayFilter", 0, 1, null)]
    public void ShouldGiveCorrectSignatureHelp(string text, int line, int character, string? expectedLabel)
    {
        var sfs = StepFactoryStore.Create();

        var document = new SCLDocument(text, DefaultUri);

        var position = new Position(line, character);

        var signatureHelp = document.GetSignatureHelp(position, sfs);

        if (expectedLabel is null)
        {
            signatureHelp.Should().BeNull("Expected Signature Help is null");
            return;
        }

        signatureHelp.Should().NotBeNull();
        signatureHelp!.Signatures.First().Label.Should().Be(expectedLabel);



    }
}