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
        NameField.text = Networking.LocalPlayerName;
        CloseButton.onClick.AddListener(() => { gameObject.SetActive(false); });
        NameField.onValueChanged.AddListener((string value) => { Networking.LocalPlayerName = value; });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
