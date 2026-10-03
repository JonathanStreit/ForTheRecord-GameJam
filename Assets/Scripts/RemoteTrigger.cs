using UnityEngine;

/// <summary>
/// One-player remote. Tapping use takes the photo, but only while in range of the camera.
/// </summary>
public class RemoteTrigger : CarryItem
{
    [SerializeField] PhotoCapture photoCamera;
    [SerializeField] float range = 8f;
    [Tooltip("The camera has to stand still on the ground to take a photo.")]
    [SerializeField] bool requireCameraResting = true;

    HeavyCamera _heavyCamera;

    protected override void Awake()
    {
        base.Awake();
        _heavyCamera = photoCamera.GetComponent<HeavyCamera>();
    }

    protected override void Activate(PlayerController player)
    {
        if (Vector3.Distance(transform.position, photoCamera.transform.position) > range)
        {
            PhotoHUD.ShowMessage("Remote is out of range!", 1.5f);
            return;
        }
        if (requireCameraResting && _heavyCamera != null && !_heavyCamera.IsResting)
        {
            PhotoHUD.ShowMessage("Put the camera down first!", 1.5f);
            return;
        }
        photoCamera.TakePhoto();
    }
}
