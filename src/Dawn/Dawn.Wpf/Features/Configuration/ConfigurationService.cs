using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Dawn.Wpf
{
    internal sealed class ConfigurationService
    {
        private readonly string _settingsFilePath;
        private readonly ConfigurationModel _configuration;

        public ConfigurationService(Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            _settingsFilePath = Path.Combine(assembly.Location.Replace(Path.GetFileName(assembly.Location), ""), "Dawn.Wpf.Settings.json");

            var location = assembly.Location.Replace(Path.GetFileName(assembly.Location), "");

            _configuration = new ConfigurationModel()
            {
                TargetFolder = @"D:\Drop\Desktop\Neuer Ordner",
                BackupFolder = Path.Combine(location, "Backups"),
                FilePattern = $"_bak{DateTime.Now:ddMMyyyy}",
                FirstStart = true,
                BackupFileTypes = new System.Collections.Generic.List<BackupFileTypeModel>()
                {
                    new BackupFileTypeModel( ".dll","DLL",true),
                    new BackupFileTypeModel( ".lst","LST",true),
                    new BackupFileTypeModel( ".exe","EXE",true)
                }
            };
        }

        public void Save()
        {
            var jsonString = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions()
            {
                IgnoreNullValues = true,
                WriteIndented = true
            });

            File.WriteAllText(_settingsFilePath, jsonString);
        }

        public ConfigurationModel Get()
        {
            if (File.Exists(_settingsFilePath))
            {
                var temp = JsonSerializer.Deserialize<ConfigurationModel>(File.ReadAllText(_settingsFilePath));

                _configuration.FirstStart = false;
                _configuration.TargetFolder = temp.TargetFolder;
                _configuration.BackupFolder = temp.BackupFolder;
                _configuration.FilePattern = temp.FilePattern;
            }

            Directory.CreateDirectory(_configuration.BackupFolder);

            return _configuration;
        }
    }
}
