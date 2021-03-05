/* --------------------------------------------------------------------------------------------
Reductech SCL Language Extension
 * ------------------------------------------------------------------------------------------ */
// tslint:disable
'use strict';
Object.defineProperty(exports, "__esModule", { value: true });
const vscode_1 = require("vscode");
const vscode_languageclient_1 = require("vscode-languageclient");
const vscode_jsonrpc_1 = require("vscode-jsonrpc");
function activate(context) {
    let serverExe = 'dotnet';
    let serverOptions = {
        run: { command: serverExe, args: [context.extensionPath  + '\\..\\Server\\Server\\bin\\Debug\\net5.0\\Server.dll'] },
        debug: { command: serverExe, args: [context.extensionPath  + '\\..\\Server\\Server\\bin\\Debug\\net5.0\\Server.dll'] }
    };
    let clientOptions = {
        documentSelector: [
            {
                pattern: '**/*.scl',
            }
        ],
        synchronize: {
            configurationSection: 'SCLLanguageServer',
            fileEvents: vscode_1.workspace.createFileSystemWatcher('**/*.scl')
        },
    };
    // Create the language client and start the client.
    const client = new vscode_languageclient_1.LanguageClient('SCLLanguageServer', 'SCL Language Server', serverOptions, clientOptions);
    client.trace = vscode_jsonrpc_1.Trace.Verbose;
    let disposable = client.start();
    // Push the disposable to the context's subscriptions so that the
    // client can be deactivated on extension deactivation
    context.subscriptions.push(disposable);
}
exports.activate = activate;
//# sourceMappingURL=extension.js.map