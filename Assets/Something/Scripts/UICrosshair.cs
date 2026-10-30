using UnityEngine;

public class UICrosshair : MonoBehaviour
{
    public RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        Cursor.visible = false;
    }

    void Update()
    {
        // Перемещаем UI-прицел точно туда, где находится мышь на экране
        rectTransform.position = Input.mousePosition;
    }
}