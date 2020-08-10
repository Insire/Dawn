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
                DeploymentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TestDeployment"),
                BackupFolder = Path.Combine(location, "Backups"),
                FirstStart = true,
                BackupFileTypes = new System.Collections.Generic.List<BackupFileTypeModel>()
                {
                    new BackupFileTypeModel( ".dll","DLL",true),
                    new BackupFileTypeModel( ".lst","LST",true),
                    new BackupFileTypeModel( ".exe","EXE",true),
                    new BackupFileTypeModel( ".json","JSON",true),
                    new BackupFileTypeModel( ".xml","XML",false),
                    new BackupFileTypeModel( ".config","CONFIG",true),
                    new BackupFileTypeModel( ".pdb","PDB",false),
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
                _configuration.DeploymentFolder = temp.DeploymentFolder;
                _configuration.BackupFolder = temp.BackupFolder;
            }

            Directory.CreateDirectory(_configuration.BackupFolder);

            return _configuration;
        }
    }
}
