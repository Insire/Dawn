using MvvmScarletToolkit.Observables;
using System;

namespace Dawn.Wpf
{
    /// <summary>
    /// a file that will be copied to a folder
    /// </summary>
    public sealed class StagingViewModel : ObservableObject
    {
        private string _path;
        public string Path
        {
            get { return _path; }
            private set { SetValue(ref _path, value); }
        }

        public StagingViewModel(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }
    }
}
