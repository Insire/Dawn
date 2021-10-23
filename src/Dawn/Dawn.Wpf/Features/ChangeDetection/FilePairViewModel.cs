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

        public void UpdateChangeState()
        {
            if (Equals(_source, _destination))
            {
                ChangeState = ChangeDetectionState.Identical;
            }
            else if (_destination is null)
            {
                ChangeState = ChangeDetectionState.Missing;
            }
            else
            {
                ChangeState = ChangeDetectionState.Changed;
            }
        }

        private static bool Equals(FileInfoViewModel one, FileInfoViewModel other)
        {
            if (other == null)
            {
                return false;
            }

            return Equals(one.Exists, other.Exists, (newValue) => one.ExistsChangeState = newValue, (newValue) => other.ExistsChangeState = newValue)
                && Equals(one.Length, other.Length, (newValue) => one.LengthChangeState = newValue, (newValue) => other.LengthChangeState = newValue)
                && Equals(one.Hash, other.Hash, (newValue) => one.HashChangeState = newValue, (newValue) => other.HashChangeState = newValue)
                && Equals(one.Attributes, other.Attributes, (newValue) => one.AttributesChangeState = newValue, (newValue) => other.AttributesChangeState = newValue)
                && Equals(one.IsNetAssembly, other.IsNetAssembly, (newValue) => one.IsNetAssemblyChangeState = newValue, (newValue) => other.IsNetAssemblyChangeState = newValue)
                && Equals(one.IsReadOnly, other.IsReadOnly, (newValue) => one.IsReadOnlyChangeState = newValue, (newValue) => other.IsReadOnlyChangeState = newValue)
                && Equals(one.CreationTime, other.CreationTime, (newValue) => one.CreationTimeChangeState = newValue, (newValue) => other.CreationTimeChangeState = newValue);
        }

        private static bool Equals<T>(T one, T other, Action<ChangeDetectionState> oneState, Action<ChangeDetectionState> otherState)
        {
            if (other is null)
            {
                oneState(ChangeDetectionState.Missing);
                otherState(ChangeDetectionState.Missing);

                return false;
            }

            if (one.Equals(other))
            {
                oneState(ChangeDetectionState.Identical);
                otherState(ChangeDetectionState.Identical);

                return true;
            }

            oneState(ChangeDetectionState.Changed);
            otherState(ChangeDetectionState.Changed);

            return false;
        }
    }
}
