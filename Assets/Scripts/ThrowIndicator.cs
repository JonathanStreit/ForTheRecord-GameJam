using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small circle on the HUD that follows one player and fills up with that player's throw power
/// (for the big camera as well as for the flash and the remote trigger).
/// </summary>
public class ThrowIndicator : MonoBehaviour
{
    [Tooltip("0 = first player that joined, 1 = second player.")]
    [SerializeField] int playerIndex;
    [Tooltip("Root of the circle graphics, hidden while this player is not charging.")]
    [SerializeField] GameObject circle;
    [Tooltip("Image with Image Type = Filled (Radial 360).")]
    [SerializeField] Image fill;
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] Color weakColor = Color.yellow;
    [SerializeField] Color strongColor = Color.red;

    Camera _view;

    void Start() => _view = Camera.main;

    void LateUpdate()
    {
        PlayerController player = playerIndex < PlayerController.All.Count ? PlayerController.All[playerIndex] : null;
        bool show = player != null && player.ThrowCharge > 0f;
        circle.SetActive(show);
        if (!show) return;

        fill.fillAmount = player.ThrowCharge;
        fill.color = Color.Lerp(weakColor, strongColor, player.ThrowCharge);
        circle.transform.position = _view.WorldToScreenPoint(player.transform.position + worldOffset);
    }
}
