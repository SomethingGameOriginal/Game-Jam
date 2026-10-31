using UnityEngine;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Звук")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string volumeParameter = "MasterVolume";
    [SerializeField] private Slider volumeSlider;

    [Header("Разрешение")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    private Resolution[] resolutions;

    private void Start()
    {
        // Список доступных разрешений без повторов.
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        int selectedIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
            if (i > 0 &&
                resolutions[i].width == resolutions[i - 1].width &&
                resolutions[i].height == resolutions[i - 1].height)
                continue;

            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                selectedIndex = options.Count - 1;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = selectedIndex;
        resolutionDropdown.RefreshShownValue();

        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("Volume", 0.75f);
            volumeSlider.value = savedVolume;
            SetVolume(savedVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            SetFullscreen(fullscreenToggle.isOn);
        }

        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    public void SetVolume(float value)
    {
        // Slider: 0–1; AudioMixer использует децибелы.
        float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;

        if (audioMixer != null)
            audioMixer.SetFloat(volumeParameter, decibels);

        PlayerPrefs.SetFloat("Volume", value);
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= resolutions.Length)
            return;

        Resolution resolution = resolutions[index];
        Screen.SetResolution(
            resolution.width,
            resolution.height,
            Screen.fullScreenMode
        );

        PlayerPrefs.SetInt("Resolution", index);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }
}