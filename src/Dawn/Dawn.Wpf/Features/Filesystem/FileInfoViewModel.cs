using System;
using System.IO;

namespace Dawn.Wpf
{
    public sealed class FileInfoViewModel : FileSystemViewModel
    {
        private string _hash;
        public string Hash
        {
            get { return _hash; }
            set { SetProperty(ref _hash, value); }
        }

        private ChangeDetectionState _hashChangeState;
        public ChangeDetectionState HashChangeState
        {
            get { return _hashChangeState; }
            set { SetProperty(ref _hashChangeState, value); }
        }

        private bool? _isNetAssembly;
        public bool? IsNetAssembly
        {
            get { return _isNetAssembly; }
            set { SetProperty(ref _isNetAssembly, value); }
        }

        private ChangeDetectionState _isNetAssemblyChangeState;
        public ChangeDetectionState IsNetAssemblyChangeState
        {
            get { return _isNetAssemblyChangeState; }
            set { SetProperty(ref _isNetAssemblyChangeState, value); }
        }

        private bool? _isReadOnly;
        public bool? IsReadOnly
        {
            get { return _isReadOnly; }
            set { SetProperty(ref _isReadOnly, value); }
        }

        private ChangeDetectionState _isReadOnlyChangeState;
        public ChangeDetectionState IsReadOnlyChangeState
        {
            get { return _isReadOnlyChangeState; }
            set { SetProperty(ref _isReadOnlyChangeState, value); }
        }

        private bool _exists;
        public bool Exists
        {
            get { return _exists; }
            set { SetProperty(ref _exists, value); }
        }

        private ChangeDetectionState _existsChangeState;
        public ChangeDetectionState ExistsChangeState
        {
            get { return _existsChangeState; }
            set { SetProperty(ref _existsChangeState, value); }
        }

        private DateTime? _updatedOn;
        /// <summary>
        /// When this class instance has been updated last
        /// </summary>
        public DateTime? UpdatedOn
        {
            get { return _updatedOn; }
            set { SetProperty(ref _updatedOn, value); }
        }

        private DateTime? _lastAccessTime;
        public DateTime? LastAccessTime
        {
            get { return _lastAccessTime; }
            set { SetProperty(ref _lastAccessTime, value); }
        }

        private DateTime? _lastWriteTime;
        public DateTime? LastWriteTime
        {
            get { return _lastWriteTime; }
            set { SetProperty(ref _lastWriteTime, value); }
        }

        private DateTime? _creationTime;
        public DateTime? CreationTime
        {
            get { return _creationTime; }
            set { SetProperty(ref _creationTime, value); }
        }

        private ChangeDetectionState _creationTimeChangeState;
        public ChangeDetectionState CreationTimeChangeState
        {
            get { return _creationTimeChangeState; }
            set { SetProperty(ref _creationTimeChangeState, value); }
        }

        private long? _length;
        /// <summary>
        /// In Bytes
        /// </summary>
        public long? Length
        {
            get { return _length; }
            set { SetProperty(ref _length, value); }
        }

        private ChangeDetectionState _lengthChangeState;
        public ChangeDetectionState LengthChangeState
        {
            get { return _lengthChangeState; }
            set { SetProperty(ref _lengthChangeState, value); }
        }

        private FileAttributes? _attributes;
        public FileAttributes? Attributes
        {
            get { return _attributes; }
            set { SetProperty(ref _attributes, value); }
        }

        private ChangeDetectionState _attributesChangeState;
        public ChangeDetectionState AttributesChangeState
        {
            get { return _attributesChangeState; }
            set { SetProperty(ref _attributesChangeState, value); }
        }

        public FileInfoViewModel(string fullPath)
            : base(fullPath, true)
        {
        }
    }
}
