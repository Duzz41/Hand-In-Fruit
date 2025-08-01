using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    // Bu metot, belirtilen sahneye geçiş yapar
    public void LoadScene(string sceneToLoad)
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void FindSource()
    {
        SoundManager.instance.FindAllAudioSources();
    }

    // Bu metot, oyunu kapatır
    public void QuitGame()
    {
#if UNITY_EDITOR
        // Oyun editördeyse, oyunu durdur
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Oyun build'deyse, oyundan çık
        Application.Quit();
#endif
    }
}
