using UnityEngine;
using UnityEngine.UI;

public class ImageAssigner : MonoBehaviour
{
    [Tooltip("The source image component (can be inactive)")]
    [SerializeField] private Image sourceImage;

    [Tooltip("The target image component (can be inactive)")]
    [SerializeField] private Image targetImage;

    private Sprite lastAssignedSprite = null; // To track changes in the source sprite

    /// <summary>
    /// Automatically assigns the sprite from the source image to the target image, even if inactive.
    /// </summary>
    private void Update()
    {
        AssignImageIfChanged();
    }

    /// <summary>
    /// Assigns the sprite from the source image to the target image if the sprite has changed.
    /// </summary>
    private void AssignImageIfChanged()
    {
        if (sourceImage == null || targetImage == null)
        {
            Debug.LogError("Source or Target Image is not assigned.");
            return;
        }

        // Ensure the source image is accessible even if inactive
        Sprite currentSprite = sourceImage.sprite;

        if (currentSprite == null)
        {
            Debug.Log("Source image sprite is null; skipping update.");
            return;
        }

        // Update the target sprite only if it has changed
        if (lastAssignedSprite != currentSprite)
        {
            bool wasTargetActive = targetImage.gameObject.activeSelf;

            if (!wasTargetActive)
            {
                targetImage.gameObject.SetActive(true); // Temporarily activate the target image
            }

            targetImage.sprite = currentSprite;
            lastAssignedSprite = currentSprite;

            Debug.Log($"Updated target image {targetImage.gameObject.name} with sprite from {sourceImage.gameObject.name}.");

            if (!wasTargetActive)
            {
                targetImage.gameObject.SetActive(false); // Restore the original active state
            }
        }
    }
}
