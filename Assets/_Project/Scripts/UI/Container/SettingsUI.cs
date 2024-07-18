using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Button _saveExitBtn;
    [SerializeField] private Slider _masterVolSlider;
    [SerializeField] private Slider _musicVolSlider;
    [SerializeField] private Slider _uiVolSlider;

    private void Awake()
    {
        // set the sliders
        _masterVolSlider.value = Config.ConfigFile.MasterVol;
        _musicVolSlider.value = Config.ConfigFile.MusicVol;
        _uiVolSlider.value = Config.ConfigFile.UIVol;

        _masterVolSlider.onValueChanged.AddListener((val) =>
        {
            Config.ConfigFile.MasterVol = val;
            Config.SaveConfigFile();
        });
        _musicVolSlider.onValueChanged.AddListener((val) =>
        {
            Config.ConfigFile.MusicVol = val;
            Config.SaveConfigFile();
        });
        _uiVolSlider.onValueChanged.AddListener((val) =>
        {
            Config.ConfigFile.UIVol = val;
            Config.SaveConfigFile();
        });

        _saveExitBtn.onClick.AddListener(() =>
        {
            // turn off the settings display
            SoundManager.instance.PlayCloseUI();
            Destroy(this.gameObject);
        });
    }
}
