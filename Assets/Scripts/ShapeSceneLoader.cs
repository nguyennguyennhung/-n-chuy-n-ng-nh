using UnityEngine;
using UnityEngine.SceneManagement;

public class ShapeSceneLoader : MonoBehaviour
{
    [SerializeField] private string targetSceneName;

    // Hàm này sẽ được gọi từ Button OnClick()
    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError($"[{name}] targetSceneName đang rỗng.");
            return   ;
        }

        SceneManager.LoadSceneAsync(targetSceneName);
    }

    // Dùng nếu mày muốn set scene bằng code từ chỗ khác
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
    }
}