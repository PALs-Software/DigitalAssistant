using System.Reflection;
using System.Runtime.Loader;

namespace DigitalAssistant.Server.Modules.Plugins;

class PluginLoadContext(string pluginPath) : AssemblyLoadContext
{
    protected AssemblyDependencyResolver Resolver = new(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = Resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath == null)
            return null;

        return LoadFromAssemblyPath(assemblyPath);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = Resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath == null)
            return IntPtr.Zero;

        return LoadUnmanagedDllFromPath(libraryPath);
    }
}
