using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerPlayerRowHandler : MonoBehaviour
{
    public TMP_Text PlayerText;
    public Button KickButton;

    public Guid PlayerId { get; private set; }

    public Action<Guid> OnPlayerKickRequest { get; set; }

    void Start()
    {
        KickButton.GetComponent<Button>().onClick.AddListener(Kick);
    }

    public void SetText(Guid id, string text)
    {
        PlayerId = id;
        PlayerText.text = text;
    }
    public void Kick()
    {
        OnPlayerKickRequest?.Invoke(PlayerId);
    }
}
