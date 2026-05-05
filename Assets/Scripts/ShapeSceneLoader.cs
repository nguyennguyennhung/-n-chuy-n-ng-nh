using UnityEngine;
using UnityEngine.SceneManagement;

public class ShapeSceneLoader : MonoBehaviour
{
    [SerializeField]
    private string targetSceneName;

    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError($"[{name}] targetSceneName đang rỗng.");
            return;
        }

        SceneManager.LoadSceneAsync(targetSceneName);
    }
}
