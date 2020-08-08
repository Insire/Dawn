using MvvmScarletToolkit.Observables;
using System.Reflection;

namespace Dawn.Wpf
{
    public sealed class ShellViewModel : ObservableObject
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            private set { SetValue(ref _title, value); }
        }

        public ConfigurationViewModel Configuration { get; }
        public UpdatesViewModel Updates { get; }

        public StagingsViewModel Stagings { get; }

        public ShellViewModel(ConfigurationViewModel configuration, UpdatesViewModel updates, StagingsViewModel stagings, Assembly assembly)
        {
            if (assembly is null)
            {
                throw new System.ArgumentNullException(nameof(assembly));
            }

            Configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
            Updates = updates ?? throw new System.ArgumentNullException(nameof(updates));
            Stagings = stagings ?? throw new System.ArgumentNullException(nameof(stagings));

            _title = $"Dawn - v{assembly.GetName().Version}";
        }
    }
}
