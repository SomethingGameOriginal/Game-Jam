using System.Collections.Generic;
using UnityEngine;
using TMPro; // Убери эту строку, если используешь обычный Dropdown вместо TMP

public class ResolutionManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown resolutionDropdown; // Замени на Dropdown, если не TMP

    private Resolution[] resolutions;

    void Start()
    {
        // Получаем все доступные разрешения экрана для данного монитора
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            // Формируем строку вида "1920 x 1080"
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // Проверяем, совпадает ли с текущим разрешением экрана
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        // Заполняем Dropdown списками и устанавливаем текущий выбор
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    // Метод, который будет вызываться при выборе пункта в UI
    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}