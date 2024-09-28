using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectorScene : MonoBehaviour
{
    void Start()
    {
        LanClientBehaviour.Instance.DisconnectedFromServer += Quit;
    }

    void OnDestroy()
    {
        LanClientBehaviour.Instance.DisconnectedFromServer -= Quit;
    }

    void Update()
    {

    }

    public void GoToController()
    {
        SceneManager.LoadSceneAsync(ScenePaths.ControllerScene);
    }

    public void Disconnect()
    {
        LanClientBehaviour.Instance.RequestDisconnect();
    }
    public void Quit()
    {
        SceneManager.LoadSceneAsync(ScenePaths.ClientConnectScene);
    }
}
