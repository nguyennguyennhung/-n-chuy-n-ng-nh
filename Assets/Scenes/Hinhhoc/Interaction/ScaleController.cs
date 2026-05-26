// === ScaleController.cs ===
using UnityEngine;

namespace AntiGravity.Interaction
{
    public class ScaleController : MonoBehaviour
    {
        public int currentStep = 1; // FIX: Giữ nguyên
        public int maxSteps = 5;    // FIX: Giữ nguyên
        
        private GeometryObject _target;

        public void PrepareScale(GeometryObject obj)
        {
            if (obj == null) return;
            _target = obj;
            currentStep = 1; // FIX-2: Reset mỗi lần vào Scale mode
        }

        /// <summary>
        /// Được gọi từ HandInputManager khi có cử chỉ nắm tay (Space hoặc Pinch).
        /// </summary>
        public void OnHandGrab(bool isInside)
        {
            if (_target == null) return;
            if (currentStep >= maxSteps) return; // FIX-2: Giới hạn 5 lần nắm liên tiếp
            currentStep++;

            if (isInside) ScaleDown();
            else ScaleUp();
        }

        // FIX: Sửa ScaleUp gọi trực tiếp từ GeometryObject (Lỗi 4d)
        public void ScaleUp() 
        { 
            if (_target != null) _target.ScaleUp(); 
        }

        // FIX: Sửa ScaleDown gọi trực tiếp từ GeometryObject (Lỗi 4e)
        public void ScaleDown() 
        { 
            if (_target != null) _target.ScaleDown(); 
        }

        // FIX: Đã xóa ApplyScale() (Lỗi 4g)
        // FIX: Đã xóa _baseScale và _isPinchingPrev (Lỗi 4a, 4b)
    }
}
