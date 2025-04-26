using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using Dawn.Avalonia.Features;
using Dawn.Avalonia.Features.Backups;
using Dawn.Avalonia.Features.Logging;
using Dawn.Core.Features.Backups;
using Dawn.Core.Features.ChangeDetection;
using Dawn.Core.Features.Filesystem;
using Dawn.Core.Features.Logging;
using System;
using System.Collections.Generic;

namespace Dawn.Avalonia
{
    public sealed class ViewLocator : IDataTemplate
    {
        private static readonly Dictionary<Type, Type> _viewmodelToViewMap = new Dictionary<Type, Type>()
        {
            [typeof(FileInfoViewModel)] = typeof(FileInfoView),
            [typeof(BackupViewModel)] = typeof(BackupView),
            [typeof(ChangeDetectionState)] = typeof(ChangeDetectionStateView),
            [typeof(FilePairViewModel)] = typeof(FilePairView),
            [typeof(LogEventViewModel)] = typeof(LogEventView),
        };

        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            var objectType = param.GetType();
            if (_viewmodelToViewMap.TryGetValue(objectType, out var viewType))
            {
                return (Control)Activator.CreateInstance(viewType)!;
            }

            var name = objectType.FullName!.Replace("ViewModel", "View", StringComparison.Ordinal).Replace("Dawn.Core.Features", "Dawn.Avalonia.Features");
            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ObservableObject;
        }
    }
}
