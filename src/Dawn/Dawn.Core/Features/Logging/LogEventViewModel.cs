using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using System.Collections.ObjectModel;

namespace Dawn.Core.Features.Logging
{
    public sealed class LogEventViewModel : ObservableObject
    {
        private readonly LogEvent _logEvent;

        private string? _text;
        public string? Text
        {
            get { return _text; }
            private set { SetProperty(ref _text, value); }
        }

        public DateTimeOffset Timestamp => _logEvent.Timestamp;
        public LogEventLevel Level => _logEvent.Level;
        public Exception? Exception => _logEvent.Exception;
        public ReadOnlyObservableCollection<KeyValuePair<string, LogEventPropertyValue>> Properties { [UsedImplicitly] get; }

        public ICommand RenderCommand { get; }

        public ICommand CopyCommand { [UsedImplicitly] get; }

        public long Key { get; }

        public LogEventViewModel(long key, LogEvent logEvent, ILogEventSink log, IClipboardService clipboardService)
        {
            Key = key;
            _logEvent = logEvent ?? throw new ArgumentNullException(nameof(logEvent));
            var log1 = log ?? throw new ArgumentNullException(nameof(log));
            var clipboardService1 = clipboardService;

            var properties = new ObservableCollection<KeyValuePair<string, LogEventPropertyValue>>(_logEvent.Properties.Select(p => new KeyValuePair<string, LogEventPropertyValue>(p.Key, p.Value)));
            Properties = new ReadOnlyObservableCollection<KeyValuePair<string, LogEventPropertyValue>>(properties);

            RenderCommand = new RelayCommand(() =>
            {
                if (Exception is not null)
                {
                    Text = Exception.ToString();
                    return;
                }

                Text = _logEvent.RenderMessage();
            });

            CopyCommand = new AsyncRelayCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(Text))
                {
                    return;
                }

                try
                {
                    await clipboardService1.SetTextAsync(Text);
                }
                catch (Exception ex)
                {
                    log1.Emit(new LogEvent(DateTimeOffset.Now, LogEventLevel.Error, ex, new MessageTemplate("Unexpected error occured, when copying data to clipboard", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));
                }
            });
        }
    }
}
