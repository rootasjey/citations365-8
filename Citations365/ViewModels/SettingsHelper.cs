using System.IO.IsolatedStorage;

namespace Citations365
{
    public static class SettingsHelper
    {
        public static readonly IsolatedStorageSettings Settings = IsolatedStorageSettings.ApplicationSettings;

        // Helper method for adding or updating a key/value pair in isolated storage
        public static bool AddOrUpdateValue(string key, object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (Settings.Contains(key))
            {
                // If the value has changed
                if (Settings[key] != value)
                {
                    // Store the new value
                    Settings[key] = value;
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                Settings.Add(key, value);
                valueChanged = true;
            }
            return valueChanged;
        }

        // Helper method for removing a key/value pair from isolated storage
        public static void RemoveValue(string key)
        {
            // If the key exists
            if (Settings.Contains(key))
            {
                Settings.Remove(key);
            }
        }

        public static void Save()
        {
            Settings.Save();
        }
    }
}