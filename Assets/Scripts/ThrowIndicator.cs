using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small circle on the HUD that follows the big camera and fills up with the throw strength.
/// </summary>
public class ThrowIndicator : MonoBehaviour
{
    [SerializeField] HeavyCamera target;
    [Tooltip("Root of the circle graphics, hidden while nobody is charging a throw.")]
    [SerializeField] GameObject circle;
    [Tooltip("Image with Image Type = Filled (Radial 360).")]
    [SerializeField] Image fill;
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);
    [SerializeField] Color weakColor = Color.yellow;
    [SerializeField] Color strongColor = Color.red;

    Camera _view;

    void Start() => _view = Camera.main;

    void LateUpdate()
    {
        float charge = target.ThrowCharge;
        circle.SetActive(charge > 0f);
        if (charge <= 0f) return;

        fill.fillAmount = charge;
        fill.color = Color.Lerp(weakColor, strongColor, charge);
        circle.transform.position = _view.WorldToScreenPoint(target.transform.position + worldOffset);
    }
}
