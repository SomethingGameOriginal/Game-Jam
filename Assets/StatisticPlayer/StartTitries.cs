using System;
using UnityEngine;

public class StartTitries : MonoBehaviour
{
    public GameObject Titrri, musik;

    public static StartTitries Instance { get; private set; }

    /// <summary>Титры начались.</summary>
    public event Action Opened;

    /// <summary>Титры закончились.</summary>
    public event Action Closed;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartTeatres()
    {
        if (IsActive) return;
        IsActive = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Titrri.SetActive(true);
        musik.SetActive(false);

        Opened?.Invoke();
    }

    public void StopTeatres()
    {
        if (!IsActive) return;
        IsActive = false;

        Titrri.SetActive(false);
        musik.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Closed?.Invoke();
    }

    private void Update()
    {
        // Страховка от чужих скриптов/Input System, которые ставят Locked.
        if (!IsActive) return;

        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;

        if (!Cursor.visible)
            Cursor.visible = true;
    }
}