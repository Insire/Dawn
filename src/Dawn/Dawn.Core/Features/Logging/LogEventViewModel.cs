using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using System.Collections.ObjectModel;

namespace Dawn.Core.Features.Logging
{
    public sealed partial class LogEventViewModel : ObservableObject
    {
        private readonly LogEvent _logEvent;
        private readonly ILogEventSink _log;
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

        public ReadOnlyObservableCollection<KeyValuePair<string, LogEventPropertyValue>> Properties { [UsedImplicitly] get; }

        public long Key { get; }

        public LogEventViewModel(long key, LogEvent logEvent, ILogEventSink log, IClipboardService clipboardService)
        {
            Key = key;
            _logEvent = logEvent ?? throw new ArgumentNullException(nameof(logEvent));
            _log = log;
            _clipboardService = clipboardService;

            var properties = new ObservableCollection<KeyValuePair<string, LogEventPropertyValue>>(_logEvent.Properties.Select(p => new KeyValuePair<string, LogEventPropertyValue>(p.Key, p.Value)));
            Properties = new ReadOnlyObservableCollection<KeyValuePair<string, LogEventPropertyValue>>(properties);
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
                : Exception.ToString();
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
                _log.Emit(new LogEvent(DateTimeOffset.Now, LogEventLevel.Error, ex, new MessageTemplate("Unexpected error occured, when copying data to clipboard", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));
            }
        }
    }
}
