using UnityEngine;
using TMPro;

public class PlayerNameManager : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField nameInputField;  // TextMeshPro Input Field for entering the name
    public TMP_Text displayNameText;      // TextMeshPro Text Field to display the name

    private const string PLAYER_NAME_KEY = "PlayerName";

    private void Start()
    {
        LoadPlayerName();
    }

    // Save the name entered in the input field
    public void SavePlayerName()
    {
        string playerName = nameInputField.text;
        if (!string.IsNullOrWhiteSpace(playerName))
        {
            PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
            PlayerPrefs.Save(); // Save changes to PlayerPrefs
            Debug.Log($"Player name saved: {playerName}");

            UpdateDisplayName(playerName); // Update the new text field
        }
        else
        {
            Debug.LogWarning("Player name is empty. Cannot save.");
        }
    }

    // Load the player name from PlayerPrefs into the input field
    private void LoadPlayerName()
    {
        if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            string savedName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
            nameInputField.text = savedName;
            UpdateDisplayName(savedName); // Update the new text field
            Debug.Log($"Player name loaded: {savedName}");
        }
        else
        {
            Debug.Log("No saved player name found.");
        }
    }

    // Update the display name text, even if the object is inactive
    private void UpdateDisplayName(string playerName)
    {
        if (displayNameText != null)
        {
            bool wasActive = displayNameText.gameObject.activeSelf; // Check if the GameObject was active
            if (!wasActive)
            {
                displayNameText.gameObject.SetActive(true); // Temporarily activate the GameObject
            }

            displayNameText.text = playerName; // Update the text with only the player's name

            if (!wasActive)
            {
                displayNameText.gameObject.SetActive(false); // Restore its inactive state
            }
        }
        else
        {
            Debug.LogWarning("Display name text field is not assigned.");
        }
    }
}
