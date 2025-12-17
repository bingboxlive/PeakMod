using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BingBox.Utils
{
    public static class DependencyLoader
    {
        private static bool _initialized;
        private static readonly Dictionary<string, Assembly> LoadedAssemblies = new Dictionary<string, Assembly>();

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly? OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var name = assemblyName.Name;

            if (LoadedAssemblies.TryGetValue(name, out var loadedAssembly))
            {
                return loadedAssembly;
            }

            var resourceName = $"BingBox.Libs.{name}.dll";

            var assembly = LoadAssemblyFromResource(resourceName);
            if (assembly != null)
            {
                LoadedAssemblies[name] = assembly;
                return assembly;
            }

            var executingAssembly = Assembly.GetExecutingAssembly();
            var resources = executingAssembly.GetManifestResourceNames();
            var match = resources.FirstOrDefault(r => r.EndsWith($".{name}.dll", StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                assembly = LoadAssemblyFromResource(match);
                if (assembly != null)
                {
                    LoadedAssemblies[name] = assembly;
                    return assembly;
                }
            }

            return null;
        }

        private static Assembly? LoadAssemblyFromResource(string resourceName)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            using (var stream = executingAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;

                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    try
                    {
                        return Assembly.Load(memoryStream.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[BingBox] Failed to load dependency {resourceName}: {ex}");
                        return null;
                    }
                }
            }
        }
    }
}
