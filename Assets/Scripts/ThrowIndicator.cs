using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small circle on the HUD that follows one of the two players carrying the big camera
/// and fills up with that player's throw power.
/// </summary>
public class ThrowIndicator : MonoBehaviour
{
    [SerializeField] HeavyCamera target;
    [Tooltip("Show the player on the right handle instead of the left one.")]
    [SerializeField] bool rightHandle;
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
        PlayerController holder = rightHandle ? target.RightHolder : target.LeftHolder;
        float charge = rightHandle ? target.RightCharge : target.LeftCharge;
        bool show = holder != null && charge > 0f;
        circle.SetActive(show);
        if (!show) return;

        fill.fillAmount = charge;
        fill.color = Color.Lerp(weakColor, strongColor, charge);
        circle.transform.position = _view.WorldToScreenPoint(holder.transform.position + worldOffset);
    }
}
