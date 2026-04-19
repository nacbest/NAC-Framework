using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Nac.EventBus.Tests")]

// Required so Castle DynamicProxy (used by NSubstitute) can proxy ILogger<T>
// where T is an internal type in this assembly.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
