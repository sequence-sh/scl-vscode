using Reductech.Sequence.Core.LanguageServer.Objects;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using InsertTextFormat = OmniSharp.Extensions.LanguageServer.Protocol.Models.InsertTextFormat;

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
            Kind = ci.Kind.ConvertEnum<Reductech.Sequence.Core.LanguageServer.Objects. CompletionItemKind, CompletionItemKind>(),
            Tags =  ci.Tags?.Select(x=>
                x.ConvertEnum<Reductech.Sequence.Core.LanguageServer.Objects.CompletionItemTag,
                    OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemTag
                >()).ToContainer(),
            AdditionalTextEdits = TextEditContainer.From(ci.AdditionalTextEdits?.Select(x=>x.ToTextEdit())) ,
            Detail = ci.Detail,
            Documentation = ci.Documentation is null? null : new MarkupContent(){Kind = MarkupKind.Markdown, Value = ci.Documentation},
            Preselect = ci.Preselect,
            SortText = ci.SortText,
            FilterText = ci.FilterText,
            InsertTextFormat = ci.InsertTextFormat?.ConvertEnum<
                Reductech.Sequence.Core.LanguageServer.Objects.InsertTextFormat,
                InsertTextFormat>()?? InsertTextFormat.PlainText,
            TextEdit = ci.TextEdit is null? null : new TextEditOrInsertReplaceEdit( ci.TextEdit.ToTextEdit()) ,
            CommitCharacters = ci.CommitCharacters?.Select(x=>x.ToString()).ToContainer() ,
            Data = ci.Data,
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
    public static TextEdit ToTextEdit(this LinePositionSpanTextChange c)
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
