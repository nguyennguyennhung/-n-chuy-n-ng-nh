// === RotationController.cs ===
using UnityEngine;

namespace AntiGravity.Interaction
{
    public class RotationController : MonoBehaviour
    {
        public float rotationSpeed = 45f;
        private GeometryObject _currentGeo;
        private bool _isRotating = false;

        public void StartRotation(GeometryObject geo)
        {
            _currentGeo = geo;
            _isRotating = true;
        }

        public void StopRotation()
        {
            _isRotating = false;
            _currentGeo = null;
        }

        private void Update()
        {
            if (_isRotating && _currentGeo != null)
            {
                // Xoay quanh trục Y theo chiều kim đồng hồ
                _currentGeo.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
