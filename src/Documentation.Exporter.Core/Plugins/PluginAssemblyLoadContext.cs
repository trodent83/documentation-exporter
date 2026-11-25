using System.Reflection;
using System.Runtime.Loader;

namespace Documentation.Exporter.Core.Plugins;

internal sealed class PluginAssemblyLoadContext(
    string pluginAssemblyPath,
    IReadOnlySet<string> sharedAssemblyNames)
    : AssemblyLoadContext(name: Path.GetFileNameWithoutExtension(pluginAssemblyPath), isCollectible: false)
{
    private readonly AssemblyDependencyResolver dependencyResolver = new(pluginAssemblyPath);
    private readonly IReadOnlySet<string> sharedNames = sharedAssemblyNames ?? throw new ArgumentNullException(nameof(sharedAssemblyNames));

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);

        if (string.IsNullOrWhiteSpace(assemblyName.Name) || sharedNames.Contains(assemblyName.Name))
        {
            return null;
        }

        var resolvedAssemblyPath = dependencyResolver.ResolveAssemblyToPath(assemblyName);
        return resolvedAssemblyPath != null
            ? LoadFromAssemblyPath(resolvedAssemblyPath)
            : null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unmanagedDllName);

        var libraryPath = dependencyResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null
            ? LoadUnmanagedDllFromPath(libraryPath)
            : nint.Zero;
    }
}
