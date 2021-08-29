using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupPanel : MonoBehaviour
{
    public Button CloseButton;
    public Text HeaderText, MessageText;
    public GameObject Holder;
    private float TimerCount = 0.0f, NeedTime = 0.0f;
    void Start()
    {
        CloseButton.onClick.AddListener(ClosePanel);
    }

    // Update is called once per frame
    void Update()
    {
        if(NeedTime > 0)
        {
            TimerCount += Time.deltaTime;
            if(TimerCount >= NeedTime) ClosePanel();
        }
    }
    public void ShowPanel(string Header, string Message, float time = 0)
    {
        Holder.SetActive(true);
        HeaderText.text = Header;
        MessageText.text = Message;
        NeedTime = time;
    }
    public void ClosePanel()
    {
        NeedTime = 0f;
        TimerCount = 0f;
        Holder.SetActive(false);
    }
}
