import {
  commands,
  workspace,
  ExtensionContext,
  ShellExecution,
  Task,
  TaskScope,
  tasks,
  window,
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
      configurationSection: 'sclLanguageServer',
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

  const sclRunCommand = () => {
    const editor = window.activeTextEditor;

    if (!editor) return;

    const config = workspace.getConfiguration('reductech-scl.edr');
    const edrPath = config.path;

    if (!edrPath) return;

    const docPath = editor.document.fileName;

    let exec = `"${edrPath}" run path "${docPath}"`;

    let task = new Task(
      { type: 'process' },
      TaskScope.Workspace,
      'Reductech EDR',
      'scl',
      new ShellExecution(exec)
    );

    tasks.executeTask(task);
  };

  context.subscriptions.push(commands.registerCommand('reductech-scl.run', sclRunCommand));
}
