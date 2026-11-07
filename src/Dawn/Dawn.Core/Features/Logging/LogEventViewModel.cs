using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Serilog.Events;
using System.Collections.ObjectModel;

namespace Dawn.Core.Features.Logging
{
    public sealed partial class LogEventViewModel : ObservableObject
    {
        private readonly LogEvent _logEvent;
        private readonly IClipboardService _clipboardService;

        private string? _text;
        public string? Text
        {
            get { return _text; }
            private set { SetProperty(ref _text, value); }
        }

        public DateTimeOffset Timestamp
            => _logEvent.Timestamp;

        public LogEventLevel Level
            => _logEvent.Level;

        public Exception? Exception
            => _logEvent.Exception;

        private ReadOnlyObservableCollection<KeyValuePair<string, LogEventPropertyValue>>? _properties;
        public ReadOnlyObservableCollection<KeyValuePair<string, LogEventPropertyValue>> Properties
        {
            [UsedImplicitly]
            get => _properties ??= new ReadOnlyObservableCollection<KeyValuePair<string, LogEventPropertyValue>>(
                new ObservableCollection<KeyValuePair<string, LogEventPropertyValue>>(_logEvent.Properties));
        }

        public long Key { get; }

        public LogEventViewModel(long key, LogEvent logEvent, IClipboardService clipboardService)
        {
            Key = key;
            _logEvent = logEvent ?? throw new ArgumentNullException(nameof(logEvent));
            _clipboardService = clipboardService;
        }

        [RelayCommand]
        private void Render()
        {
            if (Text is not null)
            {
                return;
            }

            Text = Exception is null
                ? _logEvent.RenderMessage()
                : $"{_logEvent.RenderMessage()}{Environment.NewLine}{Exception}";
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task Copy()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return;
            }

            try
            {
                await _clipboardService.SetTextAsync(Text);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error occurred, when copying data to clipboard");
            }
        }
    }
}
