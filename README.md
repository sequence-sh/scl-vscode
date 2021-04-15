## SCL vsCode

This is the VS Code extension for SCL

To use, open the folder containing this file in vsCode and press F5 (Start Debugging)

This will open another editor window with the extension enabled.

## How to Publish

- Make desired changes to LanguageServer solution
- `dotnet build`
- Check that the output was written to `Client/Server` by the postbuild step
- `cd` into the Client folder
- Update version number in package.json
- `vsce package`
- Upload the file to https://marketplace.visualstudio.com/manage/publishers/reductech