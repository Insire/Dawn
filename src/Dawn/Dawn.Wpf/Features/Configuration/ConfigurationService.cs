using Serilog;
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
        private readonly ILogger _log;

        public ConfigurationService(Assembly assembly, ILogger log)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            _log = log ?? throw new ArgumentNullException(nameof(log));

            _settingsFilePath = Path.Combine(assembly.Location.Replace(Path.GetFileName(assembly.Location), ""), "Dawn.Wpf.Settings.json");

            var location = assembly.Location.Replace(Path.GetFileName(assembly.Location), "");

            _configuration = new ConfigurationModel()
            {
                DeploymentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TestDeployment"),
                BackupFolder = Path.Combine(location, "backups"),
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

            Directory.CreateDirectory(_configuration.BackupFolder);
            Directory.CreateDirectory(Path.Combine(location, "logs"));
        }

        public void Save()
        {
            try
            {
                var jsonString = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions()
                {
                    IgnoreNullValues = true,
                    WriteIndented = true
                });

                File.WriteAllText(_settingsFilePath, jsonString);
            }
            catch (Exception ex)
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }
        }

        public ConfigurationModel Get()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var temp = JsonSerializer.Deserialize<ConfigurationModel>(File.ReadAllText(_settingsFilePath));

                    _configuration.FirstStart = false;
                    _configuration.DeploymentFolder = temp.DeploymentFolder;
                    _configuration.BackupFolder = temp.BackupFolder;
                }

                Directory.CreateDirectory(_configuration.BackupFolder);
            }
            catch (Exception ex)
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }

            return _configuration;
        }
    }
}
