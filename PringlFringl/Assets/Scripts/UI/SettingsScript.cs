using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SettingsScript : MonoBehaviour
{
    public Button CloseButton;
    public InputField NameField;
    public GameObject Holder;
    public Slider QualiySlider;
    void Start()
    {
        CloseButton.onClick.AddListener(() => { 
            gameObject.SetActive(false);
            Settings.SaveSettings();
        });
        NameField.onValueChanged.AddListener((string value) => Settings.Singleton.name = value);
        QualiySlider.onValueChanged.AddListener((float value) => 
        {
            QualitySettings.SetQualityLevel((int)value);
            Settings.Singleton.qualityLevel = (int)value;
        });
        LoadSettings();
    }

    public void LoadSettings()
    {
        NameField.text = Settings.Singleton.name;
        QualiySlider.value = Settings.Singleton.qualityLevel;
    }
}

public class Settings 
{
    public string name = "Default Name";
    public int qualityLevel = 5;
    [JsonIgnore]
    public static Settings Singleton;
    [JsonIgnore]
    const string settingsPath = "settings.json";
    public static void LoadSettings()
    {
        if (File.Exists(settingsPath))
        {
            try
            {
                Singleton = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsPath));
            }
            catch(Exception ex) { 
                Singleton = new Settings(); 
                Debug.LogError("Error loading settings " + ex); 
            }
        }
        else
        {
            Singleton = new Settings();
            SaveSettings();
        }
    }

    public static void SaveSettings()
    {
        File.WriteAllText(settingsPath, JsonConvert.SerializeObject(Singleton)); 
    }
}