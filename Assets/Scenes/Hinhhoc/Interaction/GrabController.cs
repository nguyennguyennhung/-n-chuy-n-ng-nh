// === GrabController.cs ===
using UnityEngine;

namespace AntiGravity.Interaction
{
    public class GrabController : MonoBehaviour
    {
        public Transform handAnchor;
        public float followSmooth = 15f;
        public float floorY = 0f;
        
        private GeometryObject _grabbedObject;
        private bool _isGrabbing = false;
        private Vector3 _offset;

        public void PrepareGrab(GeometryObject obj)
        {
            // FIX: Cảnh báo rõ ràng nếu thiếu Anchor
            if (handAnchor == null) {
                Debug.LogError("[GrabController] handAnchor chưa được gán trong Inspector!"); // FIX: Lỗi 6a
                return;
            }
            if (obj == null) return;
            
            _grabbedObject = obj;
            _isGrabbing = true;
            _offset = _grabbedObject.transform.position - handAnchor.position;
        }

        public void Release()
        {
            _isGrabbing = false;
            _grabbedObject = null;
        }

        private void Update()
        {
            if (!_isGrabbing || _grabbedObject == null || handAnchor == null) return;

            Vector3 targetPos = handAnchor.position + _offset;

            Collider col = _grabbedObject.GetComponent<Collider>();
            float halfH = col != null ? col.bounds.extents.y : 0.3f;
            targetPos.y = Mathf.Max(targetPos.y, floorY + halfH);

            _grabbedObject.transform.position = Vector3.Lerp(
                _grabbedObject.transform.position, targetPos, Time.deltaTime * followSmooth);
        }
    }
}
