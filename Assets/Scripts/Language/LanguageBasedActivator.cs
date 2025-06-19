using System.Collections.Generic;
using UnityEngine;
using I2.Loc;

public class LanguageBasedActivator : MonoBehaviour
{
    [System.Serializable]
    public class LanguageObject
    {
        public string languageCode; // The language code (e.g., "en", "el", etc.)
        public GameObject targetObject; // The corresponding GameObject
    }

    [Header("Language Settings")]
    public List<LanguageObject> languageObjects = new List<LanguageObject>();

    private string lastLanguage;

    private void OnEnable()
    {
        // Trigger update when the GameObject is enabled
        UpdateLanguageObjects();

        // Monitor language changes through Update (independent of external events)
        lastLanguage = LocalizationManager.CurrentLanguage;
    }

    private void Update()
    {
        // Detect changes in language dynamically
        if (LocalizationManager.CurrentLanguage != lastLanguage)
        {
            UpdateLanguageObjects();
        }
    }

    private void UpdateLanguageObjects()
    {
        // Get the current language
        string currentLanguage = LocalizationManager.CurrentLanguage;
        lastLanguage = currentLanguage;

        // Deactivate all target objects
        foreach (var langObj in languageObjects)
        {
            if (langObj.targetObject != null)
            {
                langObj.targetObject.SetActive(false);
            }
        }

        // Activate the target object for the current language
        foreach (var langObj in languageObjects)
        {
            if (langObj.languageCode.Equals(currentLanguage, System.StringComparison.OrdinalIgnoreCase))
            {
                if (langObj.targetObject != null)
                {
                    langObj.targetObject.SetActive(true);
                }
                return; // Exit loop once the correct object is found and activated
            }
        }

        // If no matching language is found, log a warning
        Debug.LogWarning($"No matching GameObject found for language: {currentLanguage}");
    }
}
