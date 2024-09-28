using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerScene : MonoBehaviour
{
    public TMP_Text m_IPText;
    public GameObject PlayerListContentObject;
    public GameObject ServerPlayerRowPrefab;

    void Start()
    {
        LanServerBehaviour.EnsureInitialized();

        LanServerBehaviour.Instance.ClientConnected += NewConnection;
        LanServerBehaviour.Instance.ClientDisconnected += LostConnection;

        m_IPText.text = $"IP: {LanCommon.GetLocalIP() ?? "(unknown)"}";
    }

    void OnDestroy()
    {
        LanServerBehaviour.Instance.ClientConnected -= NewConnection;
        LanServerBehaviour.Instance.ClientDisconnected -= LostConnection;
    }

    private void NewConnection(Guid id)
    {
        GameObject row = Instantiate(ServerPlayerRowPrefab);
        row.transform.SetParent(PlayerListContentObject.transform);
        row.GetComponent<ServerPlayerRowHandler>().SetText(id, id.ToString());
        row.GetComponent<ServerPlayerRowHandler>().OnPlayerKickRequest += KickPlayer;
    }
    private void LostConnection(Guid id)
    {
        for (int i = 0; i < PlayerListContentObject.transform.childCount; i++)
        {
            ServerPlayerRowHandler component = PlayerListContentObject.transform.GetChild(i).GetComponent<ServerPlayerRowHandler>();
            if (component.PlayerId == id)
            {
                component.OnPlayerKickRequest -= KickPlayer;
                Destroy(component.gameObject);
                break;
            }
        }
    }

    private void KickPlayer(Guid id)
    {
        LanServerBehaviour.Instance.RequestKickPlayer(id);
    }

    public void GoBack()
    {
        SceneManager.LoadScene(ScenePaths.MainMenuScene);
    }
}
