// === InteractionManager.cs ===
using UnityEngine;

namespace AntiGravity.Interaction
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [Header("Current State")]
        public InteractionMode currentMode = InteractionMode.Idle;
        public GeometryObject selectedObject;

        [Header("Controllers")]
        public WireframeController wireframeController;
        public RotationController rotationController;
        public GrabController grabController;
        public ScaleController scaleController;
        public HandInputManager inputManager;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (!wireframeController) wireframeController = GetComponent<WireframeController>();
            if (!rotationController) rotationController = GetComponent<RotationController>();
            if (!grabController) grabController = GetComponent<GrabController>();
            if (!scaleController) scaleController = GetComponent<ScaleController>();
            if (!inputManager) inputManager = GetComponent<HandInputManager>();
        }

        public void SetMode(InteractionMode mode)
        {
            if (selectedObject == null && mode != InteractionMode.Idle)
            {
                Debug.LogWarning("[InteractionManager] Chưa chọn vật thể để thực hiện lệnh.");
                return;
            }

            Debug.Log($"[InteractionManager] Chuyển Mode: {currentMode} -> {mode}");
            ExitCurrentMode();
            currentMode = mode;
            EnterNewMode();
        }

        private void ExitCurrentMode()
        {
            switch (currentMode)
            {
                case InteractionMode.Rotate:
                    rotationController.StopRotation();
                    break;
                case InteractionMode.Grab:
                    grabController.Release();
                    break;
                case InteractionMode.Scale:
                    // FIX: currentStep phải reset về 1 khi thoát Scale mode
                    if (scaleController != null) scaleController.currentStep = 1;
                    break;
            }
        }

        private void EnterNewMode()
        {
            switch (currentMode)
            {
                case InteractionMode.Idle:
                    // Khi về Idle, không nhất thiết phải ResetAll (giữ lại lựa chọn hiện tại nếu có)
                    break;
                case InteractionMode.Wireframe:
                    // FIX: Đổi EnableWireframe -> ShowWireframe theo đặc tả mới
                    wireframeController.ShowWireframe(selectedObject);
                    break;
                case InteractionMode.Rotate:
                    // FIX: Đổi EnableWireframe -> ShowWireframe
                    wireframeController.ShowWireframe(selectedObject);
                    rotationController.StartRotation(selectedObject);
                    break;
                case InteractionMode.Grab:
                    grabController.PrepareGrab(selectedObject);
                    break;
                case InteractionMode.Scale:
                    scaleController.PrepareScale(selectedObject);
                    break;
            }
        }

        public void ResetAll()
        {
            Debug.Log("[InteractionManager] Reset All - Hủy mọi chế độ.");
            if (selectedObject != null)
            {
                // FIX: Đổi DisableWireframe(obj) -> HideWireframe() (không tham số)
                wireframeController.HideWireframe();
                rotationController.StopRotation();
                grabController.Release();
                selectedObject.SetTransparency(false); // FIX: Đổi sang SetTransparency
            }
            selectedObject = null;
            currentMode = InteractionMode.Idle;
        }

        public void SelectObject(GeometryObject obj)
        {
            if (selectedObject == obj) return;
            if (selectedObject != null) ResetAll();
            
            selectedObject = obj; // FIX-2: GÁN TRƯỚC (Tránh lỗi SetMode return sớm)
            obj.Select();
            Debug.Log($"[InteractionManager] Đã chọn: {obj.name}"); // FIX-2: Log theo đặc tả
            // KHÔNG gọi SetMode ở đây — để caller tự quyết định mode (FIX-2)
        }
    }
}
