using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClientScene : MonoBehaviour
{
    public TMP_InputField IPInputField;
    public Button JoinButton;
    public GameObject ServerListContentObject;
    public GameObject ClientServerJoinButtonPrefab;
    public GameObject JoinBlockOverlayPanel;
    public TMP_Text JoinPanelIPText;

    void Start()
    {
        LanClientBehaviour.EnsureInitialized();

        LanClientBehaviour.Instance.ServerUDPMessageReceived += MessageReceived;
        LanClientBehaviour.Instance.ConnectedToServer += ClientConnected;
        LanClientBehaviour.Instance.DisconnectedFromServer += ClientDisconnected;
    }

    void OnDestroy()
    {
        LanClientBehaviour.Instance.ServerUDPMessageReceived -= MessageReceived;
        LanClientBehaviour.Instance.ConnectedToServer -= ClientConnected;
        LanClientBehaviour.Instance.DisconnectedFromServer -= ClientDisconnected;
    }

    private void MessageReceived(string ip, string message)
    {
        MainThreadRunner.Instance.Enqueue(() =>
        {
            bool found = false;
            for (int i = 0; i < ServerListContentObject.transform.childCount; i++)
            {
                ClientServerJoinButtonHandler component = ServerListContentObject.transform.GetChild(i).GetComponent<ClientServerJoinButtonHandler>();
                if (component.ServerIP.Equals(ip, StringComparison.InvariantCultureIgnoreCase))
                {
                    component.RenewKeepAlive(message);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                GameObject button = Instantiate(ClientServerJoinButtonPrefab);
                button.transform.SetParent(ServerListContentObject.transform);
                button.GetComponent<ClientServerJoinButtonHandler>().SetIP(ip, message);
                button.GetComponent<ClientServerJoinButtonHandler>().DeleteSelfRequest += OnServerKeepAliveTimeout;
                button.GetComponent<ClientServerJoinButtonHandler>().ServerJoinRequest += OnServerJoinClick;
            }
        });
    }
    private void OnServerKeepAliveTimeout(GameObject button)
    {
        button.GetComponent<ClientServerJoinButtonHandler>().DeleteSelfRequest -= OnServerKeepAliveTimeout;
        button.GetComponent<ClientServerJoinButtonHandler>().ServerJoinRequest -= OnServerJoinClick;
        Destroy(button);
    }
    private void OnServerJoinClick(string ip)
    {
        IPInputField.text = ip;
        Join();
    }

    public void GoBack()
    {
        SceneManager.LoadScene(ScenePaths.MainMenuScene);
    }

    public void Join()
    {
        if (!string.IsNullOrWhiteSpace(IPInputField.text))
        {
            bool success = LanClientBehaviour.Instance.TryStartConnect(IPInputField.text);
            if (success)
            {
                JoinButton.GetComponent<Button>().interactable = false;
                IPInputField.interactable = false;
                for (int i = 0; i < ServerListContentObject.transform.childCount; i++)
                {
                    OnServerKeepAliveTimeout(ServerListContentObject.transform.GetChild(i).gameObject);
                }
                JoinBlockOverlayPanel.SetActive(true);
                JoinPanelIPText.text = $"Trying to connect to server at {IPInputField.text}...";
            }
        }
    }

    private void ClientDisconnected()
    {
        JoinBlockOverlayPanel.SetActive(false);
        JoinButton.GetComponent<Button>().interactable = true;
        IPInputField.interactable = true;
        IPInputField.text = string.Empty;
    }
    private void ClientConnected()
    {
        SceneManager.LoadSceneAsync(ScenePaths.CharacterSelectorScene);
    }
}
