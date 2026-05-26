// === HandInputManager.cs ===
using UnityEngine;

namespace AntiGravity.Interaction
{
    public class HandInputManager : MonoBehaviour
    {
        [Header("References")]
        public InteractionManager manager;
        public OVRHand rightHand;
        public OVRHand leftHand;

        [Header("Settings")]
        public float insideDistanceThreshold = 0.2f;

        private bool _isHandInside = false;
        private GeometryObject _hoveredObject;
        private bool _isPinchingPrev = false;

        private void Update()
        {
            HandleObjectDetection();
            CheckHandProximity();
            HandleKeyboardInput();
            HandleHandTrackingInput();
        }

        private void CheckHandProximity()
        {
            // Kiểm tra vật thể gần nhất (hovered hoặc selected)
            GeometryObject target = _hoveredObject != null ? _hoveredObject : manager.selectedObject;
            if (target == null || rightHand == null) { _isHandInside = false; return; }

            float dist = Vector3.Distance(rightHand.transform.position, target.transform.position);
            _isHandInside = dist < insideDistanceThreshold;
        }

        private void HandleObjectDetection()
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                _hoveredObject = hit.collider.GetComponentInParent<GeometryObject>();
            }
            else _hoveredObject = null;
        }

        private void HandleKeyboardInput()
        {
            // Phím 1 — hủy chọn / reset về Idle
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                manager.ResetAll();
            }

            // FIX-2: Đã xóa block GetKeyUp(Alpha1) để tránh hủy chọn ngay lập tức

            // FIX: Phím Y - Giữ để xoay, buông để dừng
            if (Input.GetKeyDown(KeyCode.Y)) manager.SetMode(InteractionMode.Rotate);
            if (Input.GetKeyUp(KeyCode.Y)) manager.ResetAll(); // FIX-2: Thay SetMode(Idle) bằng ResetAll()

            // Phím 3 — Cầm hình (chỉ khi đã có hình được chọn từ trước)
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (manager.selectedObject != null)
                    manager.SetMode(InteractionMode.Grab);
            }

            // Phím 4: Chế độ Scale
            if (Input.GetKeyDown(KeyCode.Alpha4)) manager.SetMode(InteractionMode.Scale);

            // FIX: Phím Space cho Simulator Scale (Lỗi 5d)
            if (Input.GetKeyDown(KeyCode.Space) && manager.currentMode == InteractionMode.Scale)
            {
                manager.scaleController.OnHandGrab(_isHandInside);
            }
        }

        private void HandleHandTrackingInput()
        {
            if (rightHand == null || !rightHand.IsTracked) return;

            bool isPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

            // Xòe tay dừng Grab
            if (IsHandOpen(rightHand))
            {
                if (manager.currentMode == InteractionMode.Grab) manager.SetMode(InteractionMode.Idle);
            }

            _isPinchingPrev = isPinching;

            // FIX-2: Scale Quest 3: xử lý khi đang ở Scale mode
            if (manager.currentMode == InteractionMode.Scale && leftHand != null && leftHand.IsTracked)
            {
                bool rightFist = !rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
                                 !rightHand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
                bool leftFist  = !leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
                                 !leftHand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
                bool rightPoint = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
                                 !rightHand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
                bool leftPoint  = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
                                 !leftHand.GetFingerIsPinching(OVRHand.HandFinger.Middle);

                if (rightFist && leftFist && !_isPinchingPrev)
                    manager.scaleController.OnHandGrab(false);   // 2 tay nắm = ScaleUp
                else if (rightPoint && leftPoint && !_isPinchingPrev)
                    manager.scaleController.OnHandGrab(true);    // 2 tay trỏ = ScaleDown
            }
        }

        private bool IsHandOpen(OVRHand hand)
        {
            // Kiểm tra ngón trỏ và giữa không pinch
            return !hand.GetFingerIsPinching(OVRHand.HandFinger.Index) && 
                   !hand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
        }
    }
}
