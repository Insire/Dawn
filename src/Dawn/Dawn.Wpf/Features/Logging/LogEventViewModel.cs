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

        public DateTimeOffset Timestamp => _logEvent.Timestamp;
        public LogEventLevel Level => _logEvent.Level;
        public Exception Exception => _logEvent.Exception;
        public ObservableCollection<KeyValuePair<string, LogEventPropertyValue>> Properties { get; }

        public ICommand RenderCommand { get; }

        public ICommand CopyCommand { get; }

        private string _text;
        public string Text
        {
            get { return _text; }
            private set { SetProperty(ref _text, value); }
        }

        public Guid Key { get; }

        public LogEventViewModel(LogEvent logEvent, ILogEventSink log)
        {
            Key = Guid.NewGuid();
            _logEvent = logEvent ?? throw new ArgumentNullException(nameof(logEvent));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            Properties = new ObservableCollection<KeyValuePair<string, LogEventPropertyValue>>(_logEvent.Properties.Select(p => new KeyValuePair<string, LogEventPropertyValue>(p.Key, p.Value)));

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
