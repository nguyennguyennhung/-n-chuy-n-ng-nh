using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TeleportationRayActivation : MonoBehaviour
{
    public GameObject left_teleportation;
    public GameObject right_teleportation;

    public InputActionProperty left_activate;
    public InputActionProperty right_activate;

    public InputActionProperty left_cancel;
    public InputActionProperty right_cancel;

    public XRRayInteractor left_ray;
    public XRRayInteractor right_ray;

    // Update is called once per frame
    void Update()
    {
        bool is_left_ray_hovering = left_ray.TryGetHitInfo(
            out Vector3 left_position,
            out Vector3 left_normal,
            out int left_number,
            out bool left_valid
        );
        left_teleportation.SetActive(
            !is_left_ray_hovering
                && left_cancel.action.ReadValue<float>() == 0
                && left_activate.action.ReadValue<float>() > 0.1f
        );
        bool is_right_ray_hovering = right_ray.TryGetHitInfo(
            out Vector3 right_position,
            out Vector3 right_normal,
            out int right_number,
            out bool right_valid
        );
        right_teleportation.SetActive(
            !is_right_ray_hovering
                && right_cancel.action.ReadValue<float>() == 0
                && right_activate.action.ReadValue<float>() > 0.1f
        );
    }
}
