using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DotNetNamingCounter
{
    public class Program
    {
        private const string netStandardDllDirectory = @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.1.5";

        public static void Main(string[] args)
        {
            var classes = enumerateAssemblies().SelectMany(x => enumerateClasses(x)).Distinct().OrderBy(x => x.name).ToList();
            var interfaces = enumerateAssemblies().SelectMany(x => enumerateInterfaces(x)).Distinct().OrderBy(x => x.name).ToList();

            Console.WriteLine($"class: {classes.Count}");
            Console.WriteLine($"interface: {interfaces.Count}");

            var properties = classes.SelectMany(x => enumerateProperties(x.type)).Concat(interfaces.SelectMany(x => enumerateProperties(x.type))).ToList();
            var methods = classes.SelectMany(x => enumerateMethods(x.type)).Concat(interfaces.SelectMany(x => enumerateMethods(x.type))).ToList();

            Console.WriteLine($"property: {properties.Count}");
            Console.WriteLine($"method: {methods.Count}");

            var aggregatedProperties = properties.GroupBy(x => x).Select(x => new { key = x.Key, count = x.Count() }).OrderByDescending(x => x.count).ToList();
            var aggregatedMethods = methods.GroupBy(x => x).Select(x => new { key = x.Key, count = x.Count() }).OrderByDescending(x => x.count).ToList();

            Console.WriteLine($"aggregated property: {aggregatedProperties.Count}");
            Console.WriteLine($"aggregated method: {aggregatedMethods.Count}");

            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string classPath = Path.Combine(path, "class.txt");
            string interfacePath = Path.Combine(path, "interface.txt");
            string propertyPath = Path.Combine(path, "property.txt");
            string methodPath = Path.Combine(path, "method.txt");

            File.WriteAllLines(classPath, classes.Select(x => x.name).ToArray());
            File.WriteAllLines(interfacePath, interfaces.Select(x => x.name).ToArray());
            File.WriteAllLines(propertyPath, aggregatedProperties.Select(x => $"{x.key}: {x.count}").ToArray());
            File.WriteAllLines(methodPath, aggregatedMethods.Select(x => $"{x.key}: {x.count}").ToArray());
        }

        private static IEnumerable<Assembly> enumerateAssemblies()
        {
            foreach (string fileName in Directory.EnumerateFiles(netStandardDllDirectory))
            {
                if (fileName.Contains("System", StringComparison.Ordinal) == false)
                {
                    continue;
                }

                Assembly assembly = null;
                try
                {
                    assembly = Assembly.LoadFile(fileName);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                if (assembly is null)
                {
                    continue;
                }

                yield return assembly;
            }
        }

        private static IEnumerable<(Type type, string name)> enumerateClasses(Assembly assembly)
        {
            bool valid(Type type) => type.IsPublic && type.IsClass;

            foreach (var classType in assembly.GetTypes().Where(valid))
            {
                yield return (classType, classType.Name);
            }

            Type[] forwardTypes = null;
            try
            {
                forwardTypes = assembly.GetForwardedTypes();
            }
            catch (Exception e)
            {

            }

            if (forwardTypes is null)
            {
                yield break;
            }

            foreach (var classType in forwardTypes.Where(valid))
            {
                yield return (classType, classType.Name);
            }
        }

        private static IEnumerable<(Type type, string name)> enumerateInterfaces(Assembly assembly)
        {
            bool valid(Type type) => type.IsPublic && type.IsInterface;

            foreach (var interfaceType in assembly.GetTypes().Where(valid))
            {
                yield return (interfaceType, interfaceType.Name);
            }

            Type[] forwardTypes = null;
            try
            {
                forwardTypes = assembly.GetForwardedTypes();
            }
            catch (Exception e)
            {

            }

            if (forwardTypes is null)
            {
                yield break;
            }

            foreach (var interfaceType in forwardTypes.Where(valid))
            {
                yield return (interfaceType, interfaceType.Name);
            }
        }

        private static IEnumerable<string> enumerateProperties(Type type)
        {
            foreach (var property in type.GetProperties())
            {
                yield return property.Name;
            }
        }

        private static IEnumerable<string> enumerateMethods(Type type)
        {
            foreach (var method in type.GetMethods().Where(x => x.IsSpecialName == false))
            {
                yield return method.Name;
            }
        }
    }
}
