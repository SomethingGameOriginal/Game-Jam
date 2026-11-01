using UnityEngine;
using UnityEngine.UI;


public class BaseTeaching : MonoBehaviour
{
    public Sprite WASD;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Podskazka1()
    {
        CanvasTooltip.Show("Каждый выстрел приблежает вас к смерти. Буквально. Он дестабилизирует таймер на вашем поясе, ибо на более продвинутый денег не хватило, при истечении таймера, вы погибните", WASD);
    }

}
