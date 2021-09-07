using Serilog;
using System;
using System.IO;
using System.Text;

namespace Dawn.Wpf
{
    public interface IFileSystem
    {
        internal const string MetaDataFileName = "backup.json";

        bool ExtractFor<T>(string from, string to, ILogger log, DateTime timeStamp, IProgress<decimal> progress, bool overwrite = false, bool setLastWriteTime = false);

        bool MoveFor<T>(string from, string to, ILogger log, bool overwrite = false);

        bool DeleteFor<T>(string file, ILogger log);

        bool CopyFor<T>(string from, string to, ILogger log, bool overwrite = false);

        bool CopyFor<T>(string from, string to, ILogger log, DateTime timeStamp, bool overwrite = false, bool setLastWriteTime = false);

        void DeleteDirectory(string path, bool recursive);

        void DeleteFile(string path);

        bool DirectoryExists(string path);

        bool FileExists(string path);

        string[] GetDirectories(string path, string searchPattern, SearchOption searchOption);

        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);

        void CreateDirectory(string path);

        bool TrySelectFiles(out string[] files);

        bool TrySelectFolder(out string folder);

        string ReadAllText(string path, Encoding encoding);

        void WriteAllText(string path, string contents, Encoding encoding);
    }
}
