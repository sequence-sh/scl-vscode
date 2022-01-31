using System;
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.Sequence.Core.LanguageServer.Objects;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using InsertTextFormat = OmniSharp.Extensions.LanguageServer.Protocol.Models.InsertTextFormat;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace LanguageServer;

public static class Extensions
{

    public static LinePosition ToLinePosition(this Position position) => new (position.Line, position.Character);


    public static Hover ToHover(this QuickInfoResponse response) => new ()
    {
        Contents = new MarkedStringsOrMarkupContent(response.MarkdownStrings.Select(x=> new MarkedString(x))),
    };

    public static SignatureHelp ToSignatureHelp(this SignatureHelpResponse response) =>
        new()
        {
            Signatures = new Container<SignatureInformation>(response.Signatures.Select(x=>x.ToSignatureInformation())),
            ActiveParameter = response.ActiveParameter,
            ActiveSignature = response.ActiveSignature
        };


    public static SignatureInformation ToSignatureInformation(this SignatureHelpItem signatureHelpItem)
    {
        return new SignatureInformation()
        {
            Label = signatureHelpItem.Label,
            Documentation = new MarkupContent(){Kind = MarkupKind.Markdown,Value = signatureHelpItem.Documentation} ,
            Parameters =
                new Container<ParameterInformation>(
                    signatureHelpItem.Parameters.Select(x => x.ToParameterInformation())),
        };
    }


    public static ParameterInformation ToParameterInformation(this SignatureHelpParameter parameter) =>
        new () { Documentation = parameter.Documentation, Label = parameter.Label };

    public static CompletionList ToCompletionList(this CompletionResponse response) =>
        new(response.Items.Select(ToCompletionItem));

    public static CompletionItem ToCompletionItem(this Reductech.Sequence.Core.LanguageServer.Objects.CompletionItem ci)
    {
        return new CompletionItem()
        {
            Label = ci.Label,
            Kind = CompletionItemKind.Text,
            Tags =  null,
            AdditionalTextEdits = null,
            Detail = ci.Detail,
            Documentation = new MarkupContent(){Kind = MarkupKind.Markdown, Value = ci.Documentation},
            Preselect = ci.Preselect,
            SortText = null,
            FilterText = null,
            InsertTextFormat = InsertTextFormat.PlainText,
            TextEdit =  new TextEditOrInsertReplaceEdit( ci.TextEdit.ToTextEdit()) ,
            CommitCharacters = null ,
            Data = null,
        };
    }

    /// <summary>
    /// Converts an enum to an enum of another type
    /// </summary>
    public static TOut ConvertEnum<TIn, TOut>(this TIn inEnum)
    where TIn : struct, Enum
    where TOut : struct, Enum =>
        Enum.Parse<TOut>(inEnum.ToString());

    /// <summary>
    /// Converts this to a text edit
    /// </summary>
    public static TextEdit ToTextEdit(this SCLTextEdit c)
    {
        return new TextEdit()
        {
            NewText = c.NewText,
            Range = new Range(c.StartLine, c.StartColumn, c.EndLine, c.EndColumn)
        };
    }

    /// <summary>
    /// Converts an enumerable to a container
    /// </summary>
    public static Container<T> ToContainer<T>(this IEnumerable<T> enumerable) => new(enumerable);
}
