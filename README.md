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
- Install VSCE `npm install -g vsce` and make sure you npm folder is in your path
- `vsce package`
- Upload the file to https://marketplace.visualstudio.com/manage/publishers/reductech


## How to change settings

We hope to add an easier way to change this, but for now connector settings are in `\.vscode\extensions\reductech.reductech-scl-0.9.0\Server\appsettings.json`