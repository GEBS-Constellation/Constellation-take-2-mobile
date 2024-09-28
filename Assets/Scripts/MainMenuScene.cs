using UnityEngine;
using UnityEngine.SceneManagement;

public class ChooserScene : MonoBehaviour
{
    void Start()
    {
        LanClientBehaviour.EnsureDestroyed();
        LanServerBehaviour.EnsureDestroyed();
        MainThreadRunner.EnsureDestroyed();
    }

    public void ChooseServer()
    {
        SceneManager.LoadScene(ScenePaths.ServerScene);
    }

    public void ChooseClient()
    {
        SceneManager.LoadScene(ScenePaths.ClientConnectScene);
    }
}
