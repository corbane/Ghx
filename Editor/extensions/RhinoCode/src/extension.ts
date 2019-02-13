import * as vs from 'vscode'
import { execSync, exec } from "child_process"
import * as Path from "path";

var config: vs.DebugConfiguration =
{
	name: "Rhino debug session",
	type: "clr",
	request: "attach",
	requireExactSource: false,
	justMyCode: true,
	symbolOptions: {
		searchPaths: [
			"${workspaceFolder}"
		],
		searchMicrosoftSymbolServer: false
	}
}

const me = {
	extensionPath: "",
	storagePath: ""
}

export function activate(context: vs.ExtensionContext)
{
	me.extensionPath = context.extensionPath

	me.storagePath = context.storagePath || context.extensionPath

	context.subscriptions.push (vs.debug.registerDebugConfigurationProvider("clr", new RhinoConfigurationProvider()));

	context.subscriptions.push (vs.commands.registerCommand('extension.rhcode.reloadDebugger', () =>
	{
		// { "key": "f4", "command": "extension.rhcode.reloadDebugger" }

		if(vs.debug.activeDebugSession == undefined)
		{
			vs.commands.executeCommand("workbench.action.debug.start")
		}
		else
		{
			vs.commands.executeCommand("workbench.action.debug.stop")
			.then(() => {
				vs.commands.executeCommand("workbench.action.files.saveAll")
				.then(() => {
					setTimeout (() => vs.commands.executeCommand("workbench.action.debug.start"), 1000)
				})
			})
		}
	}));
}

export class RhinoConfigurationProvider implements vs.DebugConfigurationProvider 
{
	resolveDebugConfiguration (folder: vs.WorkspaceFolder | undefined, 
							   debugConfiguration: vs.DebugConfiguration, 
							   token?: vs.CancellationToken) : vs.ProviderResult <vs.DebugConfiguration>
	{
		const info = vs.window.showInformationMessage
		const warn = vs.window.showWarningMessage
		const error = vs.window.showErrorMessage
		const csharp_ext = "ms-vscode.csharp"

		if( !vs.extensions.getExtension (csharp_ext) )
		{
			const r = warn ("You must install the CSharp extension", { modal: false }, "install", "stop").then (value =>
			{
				if( value == "stop" )
					return
					
				var exe = process.argv[0];
				var combinePath = process.platform == "win32" ? Path.win32.join : Path.join
				var extdir  = combinePath (me.extensionPath, "../")
				var datadir = combinePath (me.extensionPath, "../../data/")
				
				// I don't know why, this does not work ... (Bad option: `--user-data-dir`)
				exec (`"${exe}" `
					+ `--user-data-dir "${datadir}" `
					+ `--extensions-dir "${extdir}" `
					+ `--install-extension ${csharp_ext}`, { encoding: "utf8" },
					err =>
					{
						if( err )
							info (`Please install the '${csharp_ext}' manually`)
						else
							info ("Close Visual Code and restart it")
					}
				)
			})
			return null;
		}

		if( process.platform == "win32" )
		{
			var stdout = execSync ("tasklist", { encoding: "utf8" }),
				matches = stdout.match (/Rhino.exe[ \t]+([0-9]+)/),
				pid: number

			if( matches && (pid = parseInt (matches[1])) )
				config.processId = pid
			else
				config.processId = "${command:pickProcess}"
		}
		else
		{
			//TODO: Use ps for MacOS
			config.processId = "${command:pickProcess}"
		}

		return config
	}
}

export function deactivate() {}

