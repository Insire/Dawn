using Serilog;
using System;
using System.IO;
using System.Text;

namespace Dawn.Wpf
{
    public interface IFileSystem
    {
        bool CopyFor<T>(string from, string to, ILogger log, bool overwrite = false);

        bool CopyFor<T>(string from, string to, ILogger log, DateTime timeStamp, bool overwrite = false, bool setLastWriteTime = false);

        void CreateDirectory(string path);

        void DeleteDirectory(string path, bool recursive);

        void DeleteFile(string path);

        bool DeleteFor<T>(string file, ILogger log);

        bool DirectoryExists(string path);

        bool ExtractFor<T>(string from, string to, ILogger log, DateTime timeStamp, IProgress<decimal> progress, bool overwrite = false, bool setLastWriteTime = false);

        bool FileExists(string path);

        string[] GetDirectories(string path, string searchPattern, SearchOption searchOption);

        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);

        bool MoveFor<T>(string from, string to, ILogger log, bool overwrite = false);

        string ReadAllText(string path, Encoding encoding);
        bool TrySelectFiles(out string[] files);
        bool TrySelectFolder(out string folder);
        void WriteAllText(string path, string contents, Encoding encoding);
    }
}
