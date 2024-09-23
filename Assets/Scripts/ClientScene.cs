using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientScene : MonoBehaviour
{
    public TMP_InputField IPInputField;
    public Button JoinButton;
    public GameObject ServerListContentObject;
    public GameObject ClientServerJoinButtonPrefab;

    void Start()
    {
        LanClientBehaviour.Instance.ServerUDPMessageReceived += MessageReceived;
    }

    void OnDestroy()
    {
        LanClientBehaviour.Instance.ServerUDPMessageReceived -= MessageReceived;
    }

    public void Join()
    {
        if (!string.IsNullOrWhiteSpace(IPInputField.text))
        {
            bool success = LanClientBehaviour.Instance.TryStartConnect(IPInputField.text);
            if (success)
            {
                JoinButton.GetComponent<Button>().interactable = false;
            }
        }
    }

    private void MessageReceived(string ip, string message)
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

        if (found)
        {
            GameObject button = Instantiate(ClientServerJoinButtonPrefab);
            button.transform.SetParent(ServerListContentObject.transform);
            button.GetComponent<ClientServerJoinButtonHandler>().SetIP(ip, message);
            button.GetComponent<ClientServerJoinButtonHandler>().DeleteSelfRequest += OnServerKeepAliveTimeout;
            button.GetComponent<ClientServerJoinButtonHandler>().ServerJoinRequest += OnServerJoinClick;
        }
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
}
