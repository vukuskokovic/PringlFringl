using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsScript : MonoBehaviour
{
    public Button CloseButton;
    public InputField NameField;
    void Start()
    {
        NameField.text = Networking.PlayerName;
        CloseButton.onClick.AddListener(() => { gameObject.SetActive(false); });
        NameField.onValueChanged.AddListener((string value) => { Networking.PlayerName = value; });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
