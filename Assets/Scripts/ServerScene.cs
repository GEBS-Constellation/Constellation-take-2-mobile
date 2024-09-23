using TMPro;
using UnityEngine;

public class ServerScene : MonoBehaviour
{
    public TMP_Text m_IPText;
    public TMP_InputField m_BroadcastNameInput;

    private string ip;

    void Start()
    {
        ip = LanCommon.GetLocalIP();
        m_IPText.text = $"IP: {ip ?? "(unknown)"}";
    }

    float delay = 3;
    float current = 0;
    void Update()
    {
        if (!string.IsNullOrWhiteSpace(ip)) // move to separate script?
        {
            current += Time.deltaTime;
            if (current >= delay)
            {
                current = 0;
                LanServerBehaviour.Instance.SendUdpBroadcast(m_BroadcastNameInput.text);
            }
        }
    }
}
