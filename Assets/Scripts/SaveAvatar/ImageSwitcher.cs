using UnityEngine;
using UnityEngine.UI;

public class ImageSwitcher : MonoBehaviour
{
    [Header("Image References")]
    public Image boyImage;       // Image for the boy
    public Image girlImage;      // Image for the girl
    public Image targetImage;    // The image that will be replaced dynamically

    /// <summary>
    /// Call this method to replace the target image with the boy image and represent it as "1".
    /// </summary>
    public void IsBoy()
    {
        if (boyImage != null && targetImage != null)
        {
            targetImage.sprite = boyImage.sprite;
            PlayerPrefs.SetInt("AvatarChoice", 1); // Save the choice as "1" for boy
            Debug.Log("Target image set to Boy (1).");
        }
        else
        {
            Debug.LogWarning("Boy image or target image is not assigned!");
        }
    }

    /// <summary>
    /// Call this method to replace the target image with the girl image and represent it as "2".
    /// </summary>
    public void IsGirl()
    {
        if (girlImage != null && targetImage != null)
        {
            targetImage.sprite = girlImage.sprite;
            PlayerPrefs.SetInt("AvatarChoice", 2); // Save the choice as "2" for girl
            Debug.Log("Target image set to Girl (2).");
        }
        else
        {
            Debug.LogWarning("Girl image or target image is not assigned!");
        }
    }

    /// <summary>
    /// Loads the previously saved avatar choice (1 for boy, 2 for girl) and updates the target image.
    /// </summary>
    public void LoadSavedAvatar()
    {
        int avatarChoice = PlayerPrefs.GetInt("AvatarChoice", 1); // Default to "1" for boy
        if (avatarChoice == 1 && boyImage != null)
        {
            targetImage.sprite = boyImage.sprite;
            Debug.Log("Loaded saved avatar: Boy (1).");
        }
        else if (avatarChoice == 2 && girlImage != null)
        {
            targetImage.sprite = girlImage.sprite;
            Debug.Log("Loaded saved avatar: Girl (2).");
        }
        else
        {
            Debug.LogWarning("No valid saved avatar found or images are not assigned.");
        }
    }
}
