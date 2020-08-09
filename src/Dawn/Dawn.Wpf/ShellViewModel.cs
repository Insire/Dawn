using MvvmScarletToolkit.Observables;
using System;
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

            Title = $"Dawn v{assembly.GetName().Version}";
        }
    }
}
