using UnityEngine;
using UnityEngine.UI;


public class BaseTeaching : MonoBehaviour
{
    public Sprite WASD;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CanvasTooltip.Show("Передвижение WASD", WASD);
    }

}
