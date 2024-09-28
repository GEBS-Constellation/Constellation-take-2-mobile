using UnityEngine;
using UnityEngine.SceneManagement;

public class ControllerScene : MonoBehaviour
{
    void Start()
    {
        LanClientBehaviour.Instance.DisconnectedFromServer += Quit;
    }

    void OnDestroy()
    {
        LanClientBehaviour.Instance.DisconnectedFromServer -= Quit;
    }

    public void ButtonPressed()
    {
        LanClientBehaviour.Instance.RequestSendData("A");
    }

    public void GoBack()
    {
        SceneManager.LoadScene(ScenePaths.CharacterSelectorScene);
    }

    public void Quit()
    {
        SceneManager.LoadScene(ScenePaths.ClientConnectScene);
    }
}
