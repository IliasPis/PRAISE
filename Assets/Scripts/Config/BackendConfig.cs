using UnityEngine;

[CreateAssetMenu(fileName = "BackendConfig", menuName = "Config/BackendConfig")]
public class BackendConfig : MonoBehaviour
{
    public string BackendUrl = "http://localhost:3000"; // Default to localhost
}
