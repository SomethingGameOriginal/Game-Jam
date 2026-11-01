using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreeUI : MonoBehaviour
{
    public static SkillTreeUI Instance { get; private set; }

    public event System.Action Opened;
    public event System.Action Closed;

    [Header("Ссылки на UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform nodeContainer;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private GameObject nodeButtonPrefab;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private SkillDetailsPanel detailsPanel;
    [SerializeField] private CanvasGroup hudCanvasGroup;

    [Header("Кнопки")]
    [SerializeField] private Button resetButton;
    [SerializeField] private Button centerButton;

    [Header("Управление")]
    [SerializeField] private KeyCode toggleKey = KeyCode.T;

    [Header("Панорамирование")]
    [SerializeField] private bool enablePan = true;
    [SerializeField] private float panSpeed = 1f;
    [SerializeField] private bool enableZoom = false;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2f;

    [Header("Стиль линий")]
    [SerializeField] private Color lineLockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color lineUnlockedColor = new Color(0.3f, 0.85f, 0.4f, 1f);
    [SerializeField] private float lineThickness = 4f;

    [Header("Пауза")]
    [SerializeField] private bool pauseGameWhileOpen = true;
    [SerializeField] private float pausedTimeScale = 0f;

    [Header("Дебаг")]
    [SerializeField] private bool debug = true;

    // --- runtime ---
    private SkillTreeManager manager;
    private bool built;
    private float previousTimeScale = 1f;
    private bool isPaused;
    private CursorLockMode savedLockState;
    private bool savedCursorVisible;

    private readonly Dictionary<string, SkillNodeView> nodeBySkillId = new();
    private readonly Dictionary<string, RawImage> lineByChildId = new();

    // --- pan / zoom ---
    private Vector2 panOffset;
    private float currentZoom = 1f;
    private bool panning;
    private Vector2 panStartMouse;
    private Vector2 panStartOffset;
    private Vector2 nodeContainerBasePos;
    private Vector2 lineContainerBasePos;

    // --- line texture (1×1 белый) ---
    private Texture2D lineTexture;
    private Texture2D LineTexture
    {
        get
        {
            if (lineTexture == null)
            {
                lineTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                lineTexture.SetPixel(0, 0, Color.white);
                lineTexture.Apply();
                lineTexture.filterMode = FilterMode.Point;
                lineTexture.wrapMode = TextureWrapMode.Clamp;
            }
            return lineTexture;
        }
    }

    // =========================================================
    //  LIFECYCLE
    // =========================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Dbg("Awake");
    }

    private System.Collections.IEnumerator Start()
    {
        yield return null;

        manager = SkillTreeManager.Instance;

        Dbg($"Start: manager={(manager != null ? "OK" : "NULL")}, " +
            $"skills={(manager?.Skills?.Count ?? -1)}, " +
            $"panelRoot={(panelRoot != null ? panelRoot.name : "NULL")}, " +
            $"nodeContainer={(nodeContainer != null ? nodeContainer.name : "NULL")}, " +
            $"nodeButtonPrefab={(nodeButtonPrefab != null ? nodeButtonPrefab.name : "NULL")}, " +
            $"lineContainer={(lineContainer != null ? lineContainer.name : "NULL")}, " +
            $"detailsPanel={(detailsPanel != null ? detailsPanel.name : "NULL")}");

        if (panelRoot == null || nodeContainer == null ||
            nodeButtonPrefab == null || killsText == null)
        {
            Debug.LogError("[SkillTree] Не назначены обязательные ссылки");
            enabled = false;
            yield break;
        }

        if (manager == null)
        {
            Debug.LogError("[SkillTree] SkillTreeManager.Instance = null");
            yield break;
        }

        if (manager.Skills == null || manager.Skills.Count == 0)
        {
            Debug.LogError("[SkillTree] Список Skills пуст");
            yield break;
        }

        manager.Changed -= OnManagerChanged;
        manager.Changed += OnManagerChanged;
        Dbg("Start: подписан на Changed");

        // базовые позиции контейнеров
        nodeContainerBasePos = nodeContainer.anchoredPosition;
        lineContainerBasePos = lineContainer != null
            ? lineContainer.anchoredPosition
            : Vector2.zero;

        // кнопки
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(OnResetPressed);
        }
        if (centerButton != null)
        {
            centerButton.onClick.RemoveAllListeners();
            centerButton.onClick.AddListener(OnCenterPressed);
        }

        panelRoot.SetActive(false);
        detailsPanel?.Hide();

        Rebuild();
        built = true;
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.Changed -= OnManagerChanged;

        if (lineTexture != null)
            Destroy(lineTexture);

        if (isPaused)
        {
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            isPaused = false;
        }

        if (Instance == this) Instance = null;
    }

    private void OnManagerChanged()
    {
        UpdateKills();
        UpdateAllNodes();
        UpdateAllLines();
        detailsPanel?.Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle(!panelRoot.activeSelf);

        if (panelRoot.activeSelf && enablePan)
            HandlePanInput();
    }

    // =========================================================
    //  TOGGLE / PAUSE / CURSOR
    // =========================================================
    private void Toggle(bool open)
    {
        panelRoot.SetActive(open);

        if (open)
        {
            SaveCursorState();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (pauseGameWhileOpen && !isPaused)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = pausedTimeScale;
                isPaused = true;
                Dbg($"Time.timeScale = {Time.timeScale}");
            }

            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = 0f;
                hudCanvasGroup.blocksRaycasts = false;
                hudCanvasGroup.interactable = false;
            }

            // сброс панорамирования при открытии
            panOffset = Vector2.zero;
            currentZoom = 1f;
            ApplyPan();

            if (!built) { Rebuild(); built = true; }
            UpdateKills();
            UpdateAllNodes();
            UpdateAllLines();
            Opened?.Invoke();
        }
        else
        {
            detailsPanel?.Hide();
            RestoreCursorState();

            if (pauseGameWhileOpen && isPaused)
            {
                Time.timeScale = previousTimeScale;
                isPaused = false;
                Dbg($"Time.timeScale = {Time.timeScale}");
            }

            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = 1f;
                hudCanvasGroup.blocksRaycasts = true;
                hudCanvasGroup.interactable = true;
            }

            Closed?.Invoke();
        }
    }

    private void SaveCursorState()
    {
        savedLockState = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
    }

    private void RestoreCursorState()
    {
        Cursor.lockState = savedLockState;
        Cursor.visible = savedCursorVisible;
    }

    // =========================================================
    //  PAN / ZOOM
    // =========================================================
    private void HandlePanInput()
    {
        // Начало панорамирования: СКМ / Alt+ЛКМ / ПКМ
        bool panButtonDown =
            Input.GetMouseButtonDown(2) ||
            (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftAlt)) ||
            Input.GetMouseButtonDown(1);

        if (panButtonDown && !panning)
        {
            panning = true;
            panStartMouse = Input.mousePosition;
            panStartOffset = panOffset;
        }

        // Движение
        if (panning)
        {
            bool held =
                Input.GetMouseButton(2) ||
                (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt)) ||
                Input.GetMouseButton(1);

            if (held)
            {
                Vector2 delta = (Vector2)Input.mousePosition - panStartMouse;
                panOffset = panStartOffset + delta * panSpeed;
                ApplyPan();
            }
            else
            {
                panning = false;
            }
        }

        // Зум колесом
        if (enableZoom)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float newZoom = Mathf.Clamp(currentZoom + scroll * 0.1f, minZoom, maxZoom);
                if (!Mathf.Approximately(newZoom, currentZoom))
                {
                    currentZoom = newZoom;
                    ApplyPan();
                }
            }
        }
    }

    private void ApplyPan()
    {
        if (nodeContainer != null)
        {
            nodeContainer.anchoredPosition = nodeContainerBasePos + panOffset;
            nodeContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
        }

        if (lineContainer != null)
        {
            lineContainer.anchoredPosition = lineContainerBasePos + panOffset;
            lineContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
        }
    }

    private void OnCenterPressed()
    {
        panOffset = Vector2.zero;
        currentZoom = 1f;
        ApplyPan();
        Dbg("View centered");
    }

    // =========================================================
    //  RESET
    // =========================================================
    private void OnResetPressed()
    {
        if (manager == null) return;
        manager.ResetAll();
        panOffset = Vector2.zero;
        currentZoom = 1f;
        ApplyPan();
        Dbg("Skills reset");
    }



    // =========================================================
    //  REBUILD
    // =========================================================
    private void Rebuild()
    {
        if (manager == null) return;

        Dbg($"Rebuild: skills={manager.Skills.Count}, " +
            $"старых нод={nodeContainer.childCount}");

        nodeBySkillId.Clear();
        lineByChildId.Clear();

        ClearContainer(nodeContainer);
        if (lineContainer != null) ClearContainer(lineContainer);

        foreach (var skill in manager.Skills)
        {
            if (skill == null) continue;
            CreateNode(skill);
        }
        foreach (var skill in manager.Skills)
        {
            if (skill == null || skill.Parent == null) continue;
            CreateLine(skill);
        }

        UpdateAllNodes();
        UpdateAllLines();

        Dbg($"Rebuild: нод={nodeContainer.childCount}, " +
            $"линий={(lineContainer != null ? lineContainer.childCount : -1)}");
    }

    private static void ClearContainer(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i).gameObject;
            child.transform.SetParent(null);
            Destroy(child);
        }
    }

    // =========================================================
    //  NODES
    // =========================================================
    private void CreateNode(SkillDefinition skill)
    {
        var node = Instantiate(nodeButtonPrefab, nodeContainer);
        node.name = $"Node_{skill.Id}";

        var rt = node.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = skill.TreePosition;

        var button = node.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (detailsPanel != null) detailsPanel.Show(skill);
            });
        }

        var view = node.GetComponent<SkillNodeView>();
        if (view != null) nodeBySkillId[skill.Id] = view;
        else Debug.LogError($"[SkillTree] нет SkillNodeView на префабе (id={skill.Id})");
    }

    private void UpdateAllNodes()
    {
        if (manager == null) return;

        foreach (var skill in manager.Skills)
        {
            if (skill == null) continue;
            if (!nodeBySkillId.TryGetValue(skill.Id, out var view)) continue;
            view.Bind(skill, manager.IsUnlocked(skill), manager.CanUnlock(skill));
        }
    }

    // =========================================================
    //  LINES  (RawImage + 1×1 Texture2D)
    // =========================================================
    private void CreateLine(SkillDefinition child)
    {
        if (lineContainer == null) return;

        var go = new GameObject($"Line_{child.Parent.Id}_to_{child.Id}",
            typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(lineContainer, false);

        var raw = go.GetComponent<RawImage>();
        raw.texture = LineTexture;
        raw.raycastTarget = false;
        raw.color = lineLockedColor;

        var rt = raw.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        lineByChildId[child.Id] = raw;
        go.transform.SetAsFirstSibling();
    }

    private void UpdateAllLines()
    {
        if (manager == null) return;

        int count = 0;
        foreach (var skill in manager.Skills)
        {
            if (skill == null || skill.Parent == null) continue;
            if (!lineByChildId.TryGetValue(skill.Id, out var raw) || raw == null) continue;

            raw.color = manager.IsUnlocked(skill) ? lineUnlockedColor : lineLockedColor;
            DrawLineBetween(raw.rectTransform,
                skill.Parent.TreePosition, skill.TreePosition);
            count++;
        }
        Dbg($"UpdateAllLines: обновлено {count}");
    }

    private void DrawLineBetween(RectTransform line, Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float length = dir.magnitude;
        if (length < 0.01f) { line.sizeDelta = Vector2.zero; return; }

        line.anchoredPosition = from + dir * 0.5f;
        line.sizeDelta = new Vector2(length, lineThickness);
        line.localRotation = Quaternion.Euler(0, 0,
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    // =========================================================
    //  KILLS
    // =========================================================
    private void UpdateKills()
    {
        if (killsText != null && manager != null)
            killsText.text = $"Очки навыков: {manager.TotalKills}";
    }

    private void Dbg(string msg)
    {
        if (debug) Debug.Log($"[SkillTree] {msg}");
    }
}