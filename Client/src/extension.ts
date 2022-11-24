import {
  commands,
  workspace,
  ExtensionContext,
  ShellExecution,
  Task,
  TaskScope,
  tasks,
  window,
  ProgressOptions,
} from 'vscode';
import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions,
  TransportKind,
} from 'vscode-languageclient/node';
import { Trace } from 'vscode-jsonrpc/node';

export function activate(context: ExtensionContext) {
  let serverExe = 'dotnet';

  let serverOptions: ServerOptions = {
    run: {
      command: serverExe,
      args: [context.extensionPath + '\\Server\\LanguageServer.dll'],
      transport: TransportKind.pipe,
    },
    debug: {
      command: serverExe,
      args: [context.extensionPath + '\\Server\\LanguageServer.dll'],
      transport: TransportKind.pipe,
      runtime: '',
    },
  };

  let clientOptions: LanguageClientOptions = {
    documentSelector: [
      {
        pattern: '**/*.scl',
      },
    ],
    progressOnInitialization: true,
    synchronize: {
      configurationSection: 'sequence-scl.sequence',
      fileEvents: workspace.createFileSystemWatcher('**/*.scl'),
    },
  };

  const client = new LanguageClient(
    'SCLLanguageServer',
    'SCL Language Server',
    serverOptions,
    clientOptions
  );
  client.registerProposedFeatures();
  client.trace = Trace.Verbose;
  let disposable = client.start();

  context.subscriptions.push(disposable);


  const sclRunCommand = async () => {
    const editor = window.activeTextEditor;

    if (!editor) return;
    
    const docPath = editor.document.fileName;
    let result : SCLRunResult = await client.sendRequest<SCLRunResult>("scl/runSCL", {TextDocument: docPath});

    class SCLRunResult{
      message! : string;
      success! : boolean;
    }
  };

  context.subscriptions.push(commands.registerCommand('sequence-scl.run', sclRunCommand));

  const sclStartDebuggerCommand = async () => {
    let result  = await client.sendRequest("scl/StartDebugger", {});
  }

  context.subscriptions.push(commands.registerCommand('sequence-scl.startDebugger', sclStartDebuggerCommand));

  

}
