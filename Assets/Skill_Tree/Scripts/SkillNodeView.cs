using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillNodeView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image frame;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;

    public Image Background => background;
    public Image Frame => frame;
    public Image Icon => icon;
    public TMP_Text Label => label;

    public void Bind(SkillDefinition skill,
                     bool unlocked, bool canUnlock)
    {
        if (background != null)
        {
            if (unlocked) background.color = new Color(0.3f, 0.85f, 0.4f, 1f);
            else if (canUnlock) background.color = skill.AccentColor;
            else background.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        }

        if (frame != null)
        {
            frame.sprite = skill.Frame;
            frame.enabled = skill.Frame != null;
        }

        if (icon != null)
        {
            icon.sprite = skill.Icon;
            icon.enabled = skill.Icon != null;
        }

        if (label != null)
            label.text = skill.Title;
    }
}