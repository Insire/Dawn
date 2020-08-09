using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    /// <summary>
    /// displays past updates
    /// </summary>
    public sealed class BackupsViewModel : BusinessViewModelListBase<BackupViewModel>
    {
        private readonly ConfigurationViewModel _configuration;

        public ICommand RevertCommand { get; }

        public BackupsViewModel(in IScarletCommandBuilder commandBuilder, ConfigurationViewModel configuration)
            : base(commandBuilder)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            RevertCommand = commandBuilder
                .Create(Revert, CanRevert)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();
        }

        protected override async Task RefreshInternal(CancellationToken token)
        {
            if (!Directory.Exists(_configuration.BackupFolder))
            {
                return;
            }

            var lookup = new Dictionary<string, BackupViewModel>();
            var files = await Task.Run(() => Directory.GetFiles(_configuration.BackupFolder, "*_bak*", SearchOption.TopDirectoryOnly)).ConfigureAwait(false);

            foreach (var file in files)
            {
                var split = file.Split("bak");
                if (split.Length == 2)
                {
                    var key = split[1];
                    if (key.Length >= 8)
                    {
                        int.TryParse(key.Substring(0, 2), out var days);
                        int.TryParse(key.Substring(2, 2), out var months);
                        int.TryParse(key.Substring(4, 4), out var years);

                        int.TryParse(key.Substring(8, 2), out var hours);
                        int.TryParse(key.Substring(10, 2), out var minutes);
                        int.TryParse(key.Substring(12, 2), out var seconds);

                        days--;
                        months--;
                        years--;

                        try
                        {
                            var date = DateTime.MinValue.AddDays(days);
                            date = date.AddMonths(months);
                            date = date.AddYears(years);
                            date = date.AddHours(hours);
                            date = date.AddMinutes(minutes);
                            date = date.AddSeconds(seconds);

                            key = date.ToString("yyyy.MM.dd hh:mm:ss");
                            if (!lookup.ContainsKey(key))
                            {
                                var group = new BackupViewModel(CommandBuilder, key);
                                await group.Add(new ViewModelContainer<string>(file)).ConfigureAwait(false);

                                lookup.Add(key, group);
                            }
                            else
                            {
                                await lookup[key].Add(new ViewModelContainer<string>(file)).ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            await AddRange(lookup.Values).ConfigureAwait(false);
        }

        private async Task Revert(CancellationToken token)
        {
            // check for existing backup of todays files
            // backup files if there is none
            // copy files back
            // set lastwrite timestamp
        }

        private bool CanRevert()
        {
            return false;
        }
    }
}
