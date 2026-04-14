using System.CommandLine;
using Nac.Cli.Commands;

var root = new RootCommand("NAC Framework CLI — scaffold and manage modular .NET projects");

root.Add(NewCommand.Create());
root.Add(AddCommand.Create());
root.Add(InstallCommand.Create());

return await root.Parse(args).InvokeAsync();
