using PaZword.Api.Settings;
using System;
using System.Composition;
using Windows.Storage;

namespace PaZword.Core.Settings
{
    [Export(typeof(ISettingsProvider))]
    [Shared()]
    internal sealed class SettingsProvider : ISettingsProvider
    {
        private readonly ApplicationDataContainer _roamingSettings = ApplicationData.Current.RoamingSettings;
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        public T GetSetting<T>(SettingDefinition<T> settingDefinition)
        {
            if (settingDefinition.IsRoaming)
            {
                if (_roamingSettings.Values.ContainsKey(settingDefinition.Name))
                {
                    if (typeof(T).IsEnum)
                    {
                        return (T)Enum.Parse(typeof(T), _roamingSettings.Values[settingDefinition.Name].ToString());
                    }

                    return (T)_roamingSettings.Values[settingDefinition.Name];
                }
            }
            else
            {
                if (_localSettings.Values.ContainsKey(settingDefinition.Name))
                {
                    if (typeof(T).IsEnum)
                    {
                        return (T)Enum.Parse(typeof(T), _localSettings.Values[settingDefinition.Name].ToString());
                    }

                    return (T)_localSettings.Values[settingDefinition.Name];
                }
            }

            SetSetting(settingDefinition, settingDefinition.DefaultValue);
            return settingDefinition.DefaultValue;
        }

        public void SetSetting<T>(SettingDefinition<T> settingDefinition, T value)
        {
            object valueToSave = value;
            if (value is Enum valueEnum)
            {
                valueToSave = valueEnum.ToString();
            }

            if (settingDefinition.IsRoaming)
            {
                _roamingSettings.Values[settingDefinition.Name] = valueToSave;
            }
            else
            {
                _localSettings.Values[settingDefinition.Name] = valueToSave;
            }
        }

        public void ResetSetting<T>(SettingDefinition<T> settingDefinition)
        {
            if (settingDefinition.IsRoaming)
            {
                _roamingSettings.Values.Remove(settingDefinition.Name);
            }
            else
            {
                _localSettings.Values.Remove(settingDefinition.Name);
            }
        }
    }
}
