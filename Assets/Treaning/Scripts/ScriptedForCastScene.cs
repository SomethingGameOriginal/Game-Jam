using UnityEngine;

public class ScriptedForCastScene : MonoBehaviour
{
    public Sprite WASD;
    public GameObject Hides1, Hides2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void WASDD()
    {
        CanvasTooltip.Show("Передвижение WASD, ДЭШ на ПКМ (правая кнопка мыши)", WASD);
    }

    public void Hide()
    {
        Hides1.SetActive(false);
    }
    public void Hide2()
    {
        Hides2.SetActive(false);
    }
}
