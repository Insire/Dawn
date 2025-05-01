using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static System.Environment;

namespace Dawn.Avalonia.Infrastructure
{
    public readonly record struct ShellWindowData
    {
        [JsonPropertyName("X")]
        [JsonPropertyOrder(1)]
        required public int X { get; init; }

        [JsonPropertyName("Y")]
        [JsonPropertyOrder(2)]
        required public int Y { get; init; }

        [JsonPropertyName("Width")]
        [JsonPropertyOrder(3)]
        required public double Width { get; init; }

        [JsonPropertyName("Height")]
        [JsonPropertyOrder(4)]
        required public double Height { get; init; }

        [JsonPropertyName("WindowState")]
        [JsonPropertyOrder(5)]
        required public int State { get; init; }
    }

    public sealed class JsonFileStore
    {
        private readonly ILogger _logger;

        /// <summary>
        /// The folder in which the store files will be located.
        /// </summary>
        public string FolderPath { get; }

        private JsonFileStore(ILogger logger, string storeFolderPath)
        {
            _logger = logger.ForContext<JsonFileStore>();
            FolderPath = storeFolderPath;
        }

        public JsonFileStore(ILogger logger, SpecialFolder folder)
            : this(logger, ConstructPath(folder))
        {
        }

        private static string ConstructPath(SpecialFolder baseFolder)
        {
            var text = string.Empty;
            var text2 = string.Empty;
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var assemblyCompanyAttribute = (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(entryAssembly, typeof(AssemblyCompanyAttribute))!;
                if (!string.IsNullOrEmpty(assemblyCompanyAttribute.Company))
                {
                    text = assemblyCompanyAttribute.Company + "\\";
                }

                var assemblyTitleAttribute = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(entryAssembly, typeof(AssemblyTitleAttribute))!;
                if (!string.IsNullOrEmpty(assemblyTitleAttribute.Title))
                {
                    text2 = assemblyTitleAttribute.Title + "\\";
                }
            }

            return Path.Combine(GetFolderPath(baseFolder), text + text2);
        }

        private string GetfilePath(string id)
        {
            return Path.Combine(FolderPath, id + ".json");
        }

        public T? GetData<T>(string id, CancellationToken cancellationToken = default)
        {
            var path = GetfilePath(id);
            var data = default(T);

            if (File.Exists(path))
            {
                try
                {
                    using var filestream = File.OpenRead(path);

                    data = JsonSerializer.Deserialize<T>(filestream);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unexpected error when reading data from {Path}", path);
                }
            }

            return data;
        }

        public void SetData<T>(string id, T data, CancellationToken cancellationToken = default)
        {
            var path = GetfilePath(id);

            var directoryName = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            var filemode = File.Exists(path) ? FileMode.Truncate : FileMode.Create;

            using var filestream = File.Open(path, filemode, FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(filestream, data);
        }

        public IEnumerable<string> ListIds()
        {
            return Directory.GetFiles(FolderPath, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p!);
        }
    }
}
