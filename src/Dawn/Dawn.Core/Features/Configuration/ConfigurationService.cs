using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Dawn.Core.Features.Configuration
{
    public sealed class ConfigurationService
    {
        private const string _settingsFileName = "Dawn.Wpf.Settings.json";

        private readonly string _settingsFilePath;
        private readonly ConfigurationModel _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger _log;
        private readonly IFileSystem _fileSystem;

        public ConfigurationService(ILogger log, IFileSystem fileSystem, HttpClient httpClient, Process currentProcess)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            var currentProcess1 = currentProcess ?? throw new ArgumentNullException(nameof(currentProcess));

            var path = currentProcess1.MainModule!.FileName;
            var location = path.Replace(Path.GetFileName(path), "");

            _settingsFilePath = Path.Combine(location, "Dawn.Wpf.Settings.json");

            _configuration = new ConfigurationModel()
            {
                DeploymentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TestDeployment"),
                BackupFolder = Path.Combine(location, "backups"),
                FirstStart = true,
                BackupFileTypes = new List<BackupFileTypeModel>()
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

            _fileSystem.CreateDirectory(Path.Combine(location, "logs"));
        }

        public void Save()
        {
            try
            {
                if (!_configuration.FirstStart && _configuration.IsLocalConfig != true)
                {
                    return;
                }

                var jsonString = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });

                _fileSystem.WriteAllText(_settingsFilePath, jsonString, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _log.LogError(ex);
            }
        }

        public ConfigurationModel Get()
        {
            if (string.IsNullOrEmpty(_configuration.BackupFolder))
            {
                throw new InvalidOperationException("Backup folder not set.");
            }

            var args = Environment.GetCommandLineArgs();
            foreach (var func in new[] { () => GetFromJson(args), () => GetFromUrl(args), GetFromFile })
            {
                try
                {
                    if (!func.Invoke())
                    {
                        continue;
                    }

                    _fileSystem.CreateDirectory(_configuration.BackupFolder);
                    break;
                }
                catch (FormatException)
                {
                    // when there is no base64 encoded argument
                }
                catch (Exception ex)
                {
                    _log.LogError(ex);
                }
            }

            _configuration.UpdateTimeStampOnApply ??= false;
            _configuration.UpdateTimeStampOnRestore ??= true;

            return _configuration;
        }

        private bool GetFromUrl(string[] args)
        {
            var url = args.FirstOrDefault(p => p.StartsWith("url=", StringComparison.InvariantCultureIgnoreCase));
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
                    var model = JsonSerializer.Deserialize<ConfigurationModel>(content);

                    if (model == null)
                    {
                        return false;
                    }

                    Update(_configuration, model);
                    _configuration.IsLocalConfig = false;

                    return true;
                })
                .Wait();

            return false;
        }

        private bool GetFromJson(string[] args)
        {
            var json = args.FirstOrDefault(p => p.StartsWith("json=", StringComparison.InvariantCultureIgnoreCase));
            if (json != null)
            {
                var model = JsonSerializer.Deserialize<ConfigurationModel>(GetArgument(json));
                if (model == null)
                {
                    return false;
                }

                Update(_configuration, model);
                _configuration.IsLocalConfig = false;

                return true;
            }

            return false;
        }

        private bool GetFromFile()
        {
            if (_fileSystem.FileExists(_settingsFilePath))
            {
                var json = _fileSystem.ReadAllText(_settingsFilePath, Encoding.UTF8);
                var model = JsonSerializer.Deserialize<ConfigurationModel>(json);
                if (model == null)
                {
                    return false;
                }

                Update(_configuration, model);
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
            var start = argument.IndexOf("'", StringComparison.InvariantCultureIgnoreCase);
            var end = argument.LastIndexOf("'", StringComparison.InvariantCultureIgnoreCase);
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

            if (update.BackupFileTypes.Count > 0)
            {
                target.BackupFileTypes = update.BackupFileTypes;
            }

            target.UpdateTimeStampOnApply = update.UpdateTimeStampOnApply;
            target.UpdateTimeStampOnRestore = update.UpdateTimeStampOnRestore;
            target.IsLightTheme = update.IsLightTheme;
        }
    }
}
