using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public sealed class LogEventViewModel : ObservableObject
    {
        private readonly LogEvent _logEvent;
        private readonly ILogEventSink _log;

        private string _text;
        public string Text
        {
            get { return _text; }
            private set { SetProperty(ref _text, value); }
        }

        public DateTimeOffset Timestamp => _logEvent.Timestamp;
        public LogEventLevel Level => _logEvent.Level;
        public Exception Exception => _logEvent.Exception;
        public ReadOnlyObservableCollection<KeyValuePair<string, LogEventPropertyValue>> Properties { get; }

        public ICommand RenderCommand { get; }

        public ICommand CopyCommand { get; }

        public long Key { get; }

        public LogEventViewModel(long key, LogEvent logEvent, ILogEventSink log)
        {
            Key = key;
            _logEvent = logEvent ?? throw new ArgumentNullException(nameof(logEvent));
            _log = log ?? throw new ArgumentNullException(nameof(log));

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

            CopyCommand = new RelayCommand(() =>
            {
                try
                {
                    Clipboard.SetData(DataFormats.Text, Text);
                }
                catch (Exception ex)
                {
                    _log.Emit(new LogEvent(DateTimeOffset.Now, LogEventLevel.Error, ex, new MessageTemplate("Unexpected error occured, when copying data to clipboard", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));
                }
            });
        }
    }
}
