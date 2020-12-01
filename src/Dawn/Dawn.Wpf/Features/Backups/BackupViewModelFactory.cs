using MvvmScarletToolkit;
using Serilog;
using System;

namespace Dawn.Wpf
{
    public sealed class BackupViewModelFactory
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly ILogger _log;
        private readonly IScarletCommandBuilder _commandBuilder;

        public BackupViewModelFactory(ConfigurationViewModel configurationViewModel, LogViewModel logViewModel, ILogger log, IScarletCommandBuilder commandBuilder)
        {
            _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public BackupViewModel Get(BackupModel model, BackupsViewModel backupsViewModel, Func<bool> onDeleteRequested, Action onDeleting, Func<BackupViewModel, BackupViewModel> onMetaDataEdit)
        {
            return new BackupViewModel(_commandBuilder, model, backupsViewModel, _logViewModel, _log, _configurationViewModel, onDeleteRequested, onDeleting, onMetaDataEdit);
        }
    }
}
