using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on the same GameObject as the Player Input Manager (Notification Behavior: Send Messages).
/// Places and tints each player when they join.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] Color[] playerColors = { new Color(0.2f, 0.5f, 1f), new Color(1f, 0.6f, 0.1f) };

    // Message sent by PlayerInputManager.
    void OnPlayerJoined(PlayerInput player)
    {
        int index = player.playerIndex;
        if (spawnPoints.Length > 0)
        {
            Transform spawn = spawnPoints[index % spawnPoints.Length];
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }
        if (playerColors.Length > 0)
            player.GetComponent<PlayerController>().SetColor(playerColors[index % playerColors.Length]);
    }
}
