using UnityEngine;
using UnityEngine.SceneManagement;

public class ChooserScene : MonoBehaviour
{
    void Start()
    {

    }

    void Update()
    {

    }

    public void ChooseServer()
    {
        SceneManager.LoadScene("Scenes/ServerScene");
    }

    public void ChooseClient()
    {
        SceneManager.LoadScene("Scenes/ClientScene");
    }
}
