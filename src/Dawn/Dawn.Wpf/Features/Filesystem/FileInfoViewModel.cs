using System;
using System.IO;

namespace Dawn.Wpf
{
    public class FileInfoViewModel : FileSystemViewModel
    {
        private string _hash;
        public string Hash
        {
            get { return _hash; }
            set { SetProperty(ref _hash, value); }
        }

        private bool _isNetAssembly;
        public bool IsNetAssembly
        {
            get { return _isNetAssembly; }
            set { SetProperty(ref _isNetAssembly, value); }
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { SetProperty(ref _isReadOnly, value); }
        }

        private bool _exists;
        public bool Exists
        {
            get { return _exists; }
            set { SetProperty(ref _exists, value); }
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

        private long _length;
        /// <summary>
        /// In Bytes
        /// </summary>
        public long Length
        {
            get { return _length; }
            set { SetProperty(ref _length, value); }
        }

        private FileAttributes _attributes;
        public FileAttributes Attributes
        {
            get { return _attributes; }
            set { SetProperty(ref _attributes, value); }
        }

        public FileInfoViewModel(string fullPath)
            : base(fullPath, true)
        {
        }
    }
}
