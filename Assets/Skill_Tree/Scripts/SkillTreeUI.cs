using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillTreeUI : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject panelRoot;
    public Transform nodeContainer;
    public GameObject nodeButtonPrefab;
    public TextMeshProUGUI coinsText;

    private void Start()
    {
        panelRoot.SetActive(false);
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            panelRoot.SetActive(!panelRoot.activeSelf);
            if (panelRoot.activeSelf) Refresh();
        }
    }

    public void Refresh()
    {
        foreach (Transform child in nodeContainer)
            Destroy(child.gameObject);

        var mgr = SkillTreeManager.Instance;
        coinsText.text = $"Coins: {mgr.SaveData.coins}";

        foreach (var skill in mgr.allSkills)
        {
            var go = Instantiate(nodeButtonPrefab, nodeContainer);
            go.GetComponent<RectTransform>().anchoredPosition = skill.uiPosition;

            var btn = go.GetComponent<Button>();
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            label.text = $"{skill.displayName}\n({skill.cost})";

            bool unlocked = mgr.IsUnlocked(skill.skillId);
            bool canBuy = mgr.CanUnlock(skill);

            btn.interactable = !unlocked && canBuy;

            var img = go.GetComponent<Image>();
            if (unlocked) img.color = Color.green;
            else if (canBuy) img.color = Color.white;
            else img.color = Color.gray;

            btn.onClick.AddListener(() =>
            {
                if (mgr.TryUnlock(skill))
                    Refresh();
            });
        }
    }
}