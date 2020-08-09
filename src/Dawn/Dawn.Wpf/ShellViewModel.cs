using MvvmScarletToolkit.Observables;
using System;
using System.Reflection;

namespace Dawn.Wpf
{
    public sealed class ShellViewModel : ObservableObject
    {
        public string Title { get; }

        public ConfigurationViewModel Configuration { get; }
        public UpdatesViewModel Updates { get; }

        public StagingsViewModel Stagings { get; }

        public ShellViewModel(ConfigurationViewModel configuration, UpdatesViewModel updates, StagingsViewModel stagings, Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
            Updates = updates ?? throw new System.ArgumentNullException(nameof(updates));
            Stagings = stagings ?? throw new System.ArgumentNullException(nameof(stagings));

            Title = $"Dawn - v{assembly.GetName().Version}";
        }
    }
}
