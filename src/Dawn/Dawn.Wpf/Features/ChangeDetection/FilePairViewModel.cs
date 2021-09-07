using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;

namespace Dawn.Wpf
{
    public sealed class FilePairViewModel : ObservableObject
    {
        private FileInfoViewModel _source;
        public FileInfoViewModel Source
        {
            get { return _source; }
            private set { SetProperty(ref _source, value); }
        }

        private FileInfoViewModel _destination;
        public FileInfoViewModel Destination
        {
            get { return _destination; }
            private set { SetProperty(ref _destination, value); }
        }

        private ChangeDetectionState _changeState;
        public ChangeDetectionState ChangeState
        {
            get { return _changeState; }
            private set { SetProperty(ref _changeState, value); }
        }

        public FilePairViewModel(FileInfoViewModel source, FileInfoViewModel destination)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _destination = destination ?? throw new ArgumentNullException(nameof(destination));
        }
    }
}
