using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dawn.Wpf
{
    public sealed class AboutViewModel : ViewModelListBase<Package>
    {
        public string AssemblyVersionString { get; }
        public string Copyright { get; }
        public string Product { get; }
        public Version AssemblyVersion { get; }

        public string ProjectUrl { get; }
        public string IconUrl { get; }
        public string IconAuthorUrl { get; }

        public AboutViewModel(in IScarletCommandBuilder commandBuilder, Assembly assembly)
                : base(commandBuilder)
        {
            AssemblyVersion = GetVersion(assembly);
            AssemblyVersionString = AssemblyVersion.ToString(3);
            Product = GetProduct(assembly);
            Copyright = GetCopyRight(assembly);
            ProjectUrl = "https://github.com/Insire/Dawn";
            IconUrl = "https://www.flaticon.com/free-icon/sunrise_169947";
            IconAuthorUrl = "https://www.flaticon.com/authors/freepik";

            var licenses = assembly.GetManifestResourceNames().Single(p => p.EndsWith("licenses.json"));

            using (var stream = assembly.GetManifestResourceStream(licenses))
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();

                var dpenedencies = JsonConvert.DeserializeObject<Package[]>(json);

                foreach (var package in dpenedencies)
                {
                    AddUnchecked(package);
                }
            }
        }

        private static T GetAttribute<T>(Assembly assembly)
            where T : Attribute
        {
            var attributes = assembly.GetCustomAttributes(typeof(T), false);
            return attributes.Cast<T>().First();
        }

        private static Version GetVersion(Assembly assembly)
        {
            var attribute = GetAttribute<AssemblyFileVersionAttribute>(assembly);

            return new Version(attribute.Version);
        }

        private static string GetProduct(Assembly assembly)
        {
            var attribute = GetAttribute<AssemblyProductAttribute>(assembly);
            return attribute.Product;
        }

        private static string GetCopyRight(Assembly assembly)
        {
            var attribute = GetAttribute<AssemblyCopyrightAttribute>(assembly);
            return attribute.Copyright;
        }
    }
}
