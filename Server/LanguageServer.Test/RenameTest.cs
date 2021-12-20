using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using static LanguageServer.Test.TestHelpers;

namespace LanguageServer.Test;

public class RenameTest
{

    [Theory]
    [InlineData("<hello>", 0,1, "world", "[start: (0, 0), end: (0, 7)] <world>")]
    [InlineData("<hello> <hello>", 0,1, "world", "[start: (0, 0), end: (0, 7)] <world>", "[start: (0, 8), end: (0, 15)] <world>")]
    [InlineData("<hello> something <hello>", 0,1, "world", "[start: (0, 0), end: (0, 7)] <world>", "[start: (0, 18), end: (0, 25)] <world>")]
    public void ShouldReturnCorrectRenames(string beforeText, int line, int character,string newName, params string [] expectedEdits)
    {
        var doc = new SCLDocument(beforeText, DefaultUri);

        var edits = doc.RenameVariable(new Position(line, character), newName);

        edits.Select(x => x.ToString()).Should().BeEquivalentTo(expectedEdits);

    }
}