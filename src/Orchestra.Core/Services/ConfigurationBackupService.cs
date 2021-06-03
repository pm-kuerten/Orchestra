﻿namespace Orchestra.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Catel;
    using Catel.Configuration;
    using Catel.Logging;
    using Catel.Reflection;
    using Catel.Services;
    using MethodTimer;
    using Orc.FileSystem;

    public class ConfigurationBackupService : IConfigurationBackupService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IConfigurationService _configurationService;
        private readonly IAppDataService _appDataService;
        private readonly IFileService _fileService;
        private readonly IDirectoryService _directoryService;

        public ConfigurationBackupService(IConfigurationService configurationService, IAppDataService appDataService, IFileService fileService, IDirectoryService directoryService)
        {
            Argument.IsNotNull(() => configurationService);
            Argument.IsNotNull(() => appDataService);
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNull(() => directoryService);

            _configurationService = configurationService;
            _appDataService = appDataService;
            _fileService = fileService;
            _directoryService = directoryService;
        }

        public int NumberOfBackups { get; protected set; } = 10;

        public string BackupTimeStampFormat { get; set; } = "{0:yyyyMMdd_HHmmssfff}";

        [Time]
        public virtual async Task BackupAsync()
        {
            try
            {
                // Get configuration paths
                var roamingConfigFilePathField = _configurationService.GetType().GetFieldEx("_roamingConfigFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var localConfigFilePathField = _configurationService.GetType().GetFieldEx("_localConfigFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var roamingConfigFilePath = roamingConfigFilePathField.GetValue(_configurationService).ToString();
                var localConfigFilePath = localConfigFilePathField.GetValue(_configurationService).ToString();

                await BackupConfigurationAsync(roamingConfigFilePath, Catel.IO.ApplicationDataTarget.UserRoaming);
                await BackupConfigurationAsync(localConfigFilePath, Catel.IO.ApplicationDataTarget.UserLocal);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create configuration backup");
                throw;
            }
        }

        protected virtual async Task BackupConfigurationAsync(string configurationFilePath, Catel.IO.ApplicationDataTarget applicationDataTarget)
        {
            if (!_fileService.Exists(configurationFilePath))
            {
                Log.Debug($"Configuration file not found on path {configurationFilePath}, skipping backup");
            }

            Log.Info($"Creating configuration backup, {applicationDataTarget}");

            var configBackupFolderPath = Path.Combine(_appDataService.GetApplicationDataDirectory(Catel.IO.ApplicationDataTarget.UserRoaming), "backup", "config");

            _directoryService.Create(configBackupFolderPath);

            var backupConfigurationFiles = _directoryService.GetFiles(configBackupFolderPath, "configuration*").OrderBy(f => f).ToList();
            if (backupConfigurationFiles.Count >= NumberOfBackups)
            {
                // Remove oldest backup
                var filesToRemoveCount = 1 + NumberOfBackups - backupConfigurationFiles.Count;

                for (int i = 0; i < filesToRemoveCount; i++)
                {
                    var backupFilePath = Path.Combine(configBackupFolderPath, backupConfigurationFiles[i]);
                    _fileService.Delete(backupFilePath);
                }
            }

            // Save current configuration
            _fileService.Copy(configurationFilePath, Path.Combine(configBackupFolderPath, $"configuration.{string.Format(BackupTimeStampFormat, DateTime.Now)}.xml"));

            Log.Info($"Created configuration backup, {applicationDataTarget}");
        }
    }
}
