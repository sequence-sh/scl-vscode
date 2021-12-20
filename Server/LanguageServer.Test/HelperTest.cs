using System.Linq;
using FluentAssertions;
using Xunit;

namespace LanguageServer.Test;

public class HelperTest
{
    [Theory]
    [InlineData("print 123","print 123:0,0")]
    [InlineData("- print 123","- print 123:0,0")]
    [InlineData("- print 123\r\n- print 456\r\n- print 789","- print 123:0,0","- print 456:1,0","- print 789:2,0")]
    public void TestCommandSplitting(string text, params string[] expected)
    {
        var results = Helpers.SplitIntoCommands(text);

        var actual= results.Select(x => $"{x.text}:{x.position.Line},{x.position.Character}").ToList();

        actual.Should().BeEquivalentTo(expected);
    }
}