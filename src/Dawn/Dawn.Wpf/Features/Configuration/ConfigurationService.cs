using ImTools;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Dawn.Wpf
{
    public sealed class ConfigurationService
    {
        private const string _settingsFileName = "Dawn.Wpf.Settings.json";

        private readonly string _settingsFilePath;
        private readonly ConfigurationModel _configuration;
        private readonly HttpClient _httpClient;
        private readonly Process _currentProcess;
        private readonly ILogger _log;

        public ConfigurationService(ILogger log, HttpClient httpClient, Process currentProcess)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _currentProcess = currentProcess ?? throw new ArgumentNullException(nameof(currentProcess));

            var path = _currentProcess.MainModule.FileName;
            var location = path.Replace(Path.GetFileName(path), "");

            _settingsFilePath = Path.Combine(location, "Dawn.Wpf.Settings.json");

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
                    new BackupFileTypeModel( ".zip","ZIP",true),
                }
            };

            Directory.CreateDirectory(Path.Combine(location, "logs"));
        }

        public void Save()
        {
            try
            {
                if (!_configuration.FirstStart && !_configuration.IsLocalConfig)
                {
                    return;
                }

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
            var args = Environment.GetCommandLineArgs();
            foreach (var func in new Func<bool>[] { () => GetFromJson(args), () => GetFromUrl(args), () => GetFromFile() })
            {
                try
                {
                    if (func.Invoke())
                    {
                        Directory.CreateDirectory(_configuration.BackupFolder);
                        break;
                    }
                }
                catch (FormatException)
                {
                    // when there is no base64 encoded argument
                }
                catch (Exception ex)
                {
                    _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
                }
            }

            return _configuration;
        }

        private bool GetFromUrl(string[] args)
        {
            var url = args.FindFirst(p => p.StartsWith("url="));
            if (url == null)
            {
                return false;
            }

            if (!Uri.TryCreate(GetArgument(url), UriKind.Absolute, out var baseAddress))
            {
                return false;
            }

            _httpClient.BaseAddress = baseAddress;
            _httpClient
                .GetAsync(_settingsFileName)
                .ContinueWith(async t =>
                {
                    t.Result.EnsureSuccessStatusCode();

                    var content = await t.Result.Content.ReadAsStringAsync().ConfigureAwait(false);

                    Update(_configuration, JsonSerializer.Deserialize<ConfigurationModel>(content));
                    _configuration.IsLocalConfig = false;

                    return true;
                })
                .Wait();

            return false;
        }

        private bool GetFromJson(string[] args)
        {
            var json = args.FindFirst(p => p.StartsWith("json="));
            if (json != null)
            {
                Update(_configuration, JsonSerializer.Deserialize<ConfigurationModel>(GetArgument(json)));
                _configuration.IsLocalConfig = false;

                return true;
            }

            return false;
        }

        private bool GetFromFile()
        {
            if (File.Exists(_settingsFilePath))
            {
                Update(_configuration, JsonSerializer.Deserialize<ConfigurationModel>(File.ReadAllText(_settingsFilePath)));
                _configuration.IsLocalConfig = true;

                return true;
            }

            return false;
        }

        /// <summary>
        /// converts cli argument into clear text value
        /// </summary>
        /// <param name="argument">needs to be base64 encoded and the source of that needs to be utf8 encoded</param>
        private static string GetArgument(string argument)
        {
            var start = argument.IndexOf("'");
            var end = argument.LastIndexOf("'");
            var length = end - start;

            var result = argument.Substring(start + 1, length - 1);
            var bytes = Convert.FromBase64String(result);

            return Encoding.UTF8.GetString(bytes);
        }

        private static void Update(ConfigurationModel target, ConfigurationModel update)
        {
            target.FirstStart = false;

            if (update.DeploymentFolder?.Length > 0)
            {
                target.DeploymentFolder = update.DeploymentFolder;
            }

            if (update.BackupFolder?.Length > 0)
            {
                target.BackupFolder = update.BackupFolder;
            }

            if (update.BackupFileTypes?.Count > 0)
            {
                target.BackupFileTypes = update.BackupFileTypes;
            }

            target.IsLightTheme = update.IsLightTheme;
        }
    }
}
