using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;

namespace LanguageServer;

/// <summary>
/// Listens for errors in the SCL
/// </summary>
public class ErrorErrorListener : IAntlrErrorListener<IToken>
{
    /// <summary>
    /// The errors which have been found
    /// </summary>
    public List<SingleError> Errors = new();

    /// <inheritdoc />
    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {

        TextLocation textLocation;
        if (e is NoViableAltException noViableAltException)
        {
            textLocation = new TextLocation(noViableAltException.StartToken.Text,  //not the correct text
                new TextPosition(noViableAltException.StartToken),
                new TextPosition(offendingSymbol));
        }
        else
        {
            textLocation = new TextLocation(offendingSymbol);
        }

        var errorBuilder = ErrorCode.SCLSyntaxError.ToErrorBuilder(msg);
        var error = errorBuilder.WithLocationSingle(textLocation);
        Errors.Add(error);
    }
}