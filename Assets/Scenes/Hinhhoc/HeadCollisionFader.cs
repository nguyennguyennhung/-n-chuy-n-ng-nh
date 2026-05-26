using UnityEngine;

public class HeadCollisionFader : MonoBehaviour
{
    public LayerMask wallLayer; // Chỉ định layer của tường/vật cản
    private OVRScreenFade _fader;

    void Start()
    {
        _fader = GetComponentInParent<OVRScreenFade>();
    }

    void OnTriggerEnter(Collider other)
    {
        // Nếu đầu chui vào vật cản thuộc wallLayer
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            if (_fader != null) _fader.FadeOut(); // Màn hình tối đen
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            if (_fader != null) _fader.FadeIn(); // Trở lại bình thường
        }
    }
}
