using UnityEngine;

public class StartTitries : MonoBehaviour
{
    public GameObject Titrri;

    public void StartTeatres()
    {
        Cursor.lockState = CursorLockMode.None;
        Titrri.SetActive(true);
    }

}
