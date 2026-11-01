using UnityEngine;

public class TrigerToolTip : MonoBehaviour
{
    public Sprite WASD;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            CanvasTooltip.Show("Каждый выстрел приблежает вас к смерти. Буквально. Он дестабилизирует таймер на вашем поясе, ибо на более продвинутый денег не хватило, при истечении таймера, вы погибните", WASD);
        }
    }
}
