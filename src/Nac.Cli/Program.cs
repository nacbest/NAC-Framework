using System.CommandLine;
using Nac.Cli.Commands;

var rootCommand = new RootCommand("NAC Framework CLI");
rootCommand.AddCommand(NewCommand.Create());

return await rootCommand.InvokeAsync(args);
