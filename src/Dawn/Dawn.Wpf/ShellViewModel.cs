using MvvmScarletToolkit.Observables;
using System;
using System.Linq;
using System.Reflection;

namespace Dawn.Wpf
{
    public sealed class ShellViewModel : ObservableObject
    {
        public string Title { get; }

        public ConfigurationViewModel Configuration { get; }
        public BackupsViewModel Updates { get; }

        public StagingsViewModel Stagings { get; }

        public ShellViewModel(ConfigurationViewModel configuration, BackupsViewModel updates, StagingsViewModel stagings, Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Updates = updates ?? throw new ArgumentNullException(nameof(updates));
            Stagings = stagings ?? throw new ArgumentNullException(nameof(stagings));

            Title = $"Dawn v{GetVersion(assembly)}";
        }

        private string GetVersion(Assembly assembly)
        {
            var attributes = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
            if (attributes.Length == 0)
            {
                return assembly.GetName().Version?.ToString(3);
            }

            var version = attributes.Cast<AssemblyMetadataAttribute>().FirstOrDefault(p => p.Key == "Version");

            return new Version(version.Value).ToString(3);
        }
    }
}
