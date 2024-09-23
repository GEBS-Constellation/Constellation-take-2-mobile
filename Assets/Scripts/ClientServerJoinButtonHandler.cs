using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientServerJoinButtonHandler : MonoBehaviour
{
    public Button Button;

    public Action<string> ServerJoinRequest { get; set; }
    public Action<GameObject> DeleteSelfRequest { get; set; }

    public string ServerIP { get; private set; }

    private const float keepAliveTime = 5;
    private float currentTime = 0;

    void Start()
    {
        Button.GetComponent<Button>().onClick.AddListener(OnServerJoinClick);
    }

    void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime >= keepAliveTime)
        {
            DeleteSelfRequest.Invoke(gameObject);
        }
    }

    public void SetIP(string ip, string message)
    {
        ServerIP = ip;
        RenewKeepAlive(message);
    }

    public void RenewKeepAlive(string message)
    {
        currentTime = 0;
        Button.transform.GetChild(0).GetComponent<TMP_Text>().text = $"{message} ({ServerIP})";
    }

    private void OnServerJoinClick()
    {
        ServerJoinRequest.Invoke(ServerIP);
    }
}
