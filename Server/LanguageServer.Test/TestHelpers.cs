using OmniSharp.Extensions.LanguageServer.Protocol;

namespace LanguageServer.Test;

public static class TestHelpers
{
    public static readonly DocumentUri DefaultUri = new(null, null, null, null, null);


    public const string LongText = @"[(artist: 'Blake, Robert' artistid: 123)]
| ArrayFilter ((from <entity> 'artist') == 'Blake, Robert')
| EntityMap (in <entity> 'artist' (StringToCase (from <entity> 'artist') TextCase.Upper ))
| EntityMapProperties (artist: 'Artist Name' artistId: 'ArtistId')
| ArraySort (from <entity> 'year')
| ArrayDistinct (from <entity> 'id')
| print
";
}