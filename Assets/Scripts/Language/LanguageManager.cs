using System.Collections.Generic;
using UnityEngine;
using I2.Loc;

public class LanguageManager : MonoBehaviour
{
    public static string SelectedLanguage { get; private set; } = "EN"; // Default language

    // Map full language names to their codes
    private readonly Dictionary<string, string> languageMap = new Dictionary<string, string>
    {
        { "English", "EN" },
        { "Swedish", "SW" },
        { "Italian", "IT" },
        { "Greek", "GR" },
        { "Romanian", "RO" }
    };

    void Start()
    {
        // Ensure the correct language is set on start
        ChangeLanguage(LocalizationManager.CurrentLanguage);
    }

    /// <summary>
    /// Changes the language and refreshes the UI.
    /// </summary>
    /// <param name="languageName">The name of the language to switch to.</param>
    public void ChangeLanguage(string languageName)
    {
        // Attempt to map the full language name to its code
        if (languageMap.TryGetValue(languageName, out string languageCode))
        {
            SelectedLanguage = languageCode; // Store the selected language code
            LocalizationManager.CurrentLanguage = languageName;
            Debug.Log($"Language successfully changed to: {languageName} ({languageCode})");

            // Force a refresh on all localized components
            LocalizationManager.LocalizeAll(true);
        }
        else
        {
            Debug.LogError($"Language '{languageName}' is not available. Please add it to the language map.");
        }
    }
}
