// Editor/SkillTreeLayoutEditor.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SkillTreeLayoutEditor : EditorWindow
{
    private const float GridStep = 80f;
    private const float NodeW = 140f;
    private const float NodeH = 60f;

    // «Мировые» границы — от -WORLD/2 до +WORLD/2
    private const float WorldHalfExtent = 4000f;

    private Vector2 scroll;
    private float zoom = 1f;
    private Vector2 pan;          // смещение «мира» в экранных пикселях

    private SkillDefinition[] allSkills;
    private readonly List<SkillDefinition> placed = new();

    private SkillDefinition dragging;
    private Vector2 dragOffset;

    private bool panning;
    private Vector2 panStartMouse;
    private Vector2 panStartValue;

    private SkillDefinition linkFrom;
    private bool linkMode;

    [MenuItem("Tools/Skills/Open Layout Editor")]
    public static void Open() => GetWindow<SkillTreeLayoutEditor>("Skill Layout");

    private void OnEnable()
    {
        RefreshAssets();
        // стартовый pan — чтобы (0,0) был в центре окна
        if (position.width > 1)
            pan = new Vector2(position.width * 0.5f, position.height * 0.5f);
    }

    private void RefreshAssets()
    {
        var guids = AssetDatabase.FindAssets("t:SkillDefinition");
        allSkills = new SkillDefinition[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            allSkills[i] = AssetDatabase.LoadAssetAtPath<SkillDefinition>(
                AssetDatabase.GUIDToAssetPath(guids[i]));

        placed.Clear();
        foreach (var s in allSkills)
        {
            if (s == null) continue;
            if (s.StartingSkill || s.TreePosition != Vector2.zero)
                placed.Add(s);
        }
        foreach (var s in allSkills)
        {
            if (s == null || s.Parent == null) continue;
            if (!placed.Contains(s)) placed.Add(s);
            if (!placed.Contains(s.Parent)) placed.Add(s.Parent);
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (linkMode)
            EditorGUILayout.HelpBox(
                linkFrom == null
                    ? "Кликни родителя, потом ребёнка. ПКМ — отмена."
                    : $"Связь от «{linkFrom.Title}». Кликни ребёнка. ПКМ — отмена.",
                MessageType.Info);

        // 1) Input ДО отрисовки — иначе теряются события
        HandleInput();

        // 2) ScrollView — просто даёт вертикальную прокрутку, если контент больше окна.
        //    Но у нас своя pan-система, поэтому можно обойтись БЕЗ ScrollView.
        //    Используем BeginArea на весь экран, чтобы рисовать всё в screen-space.
        var area = new Rect(0, 0, position.width, position.height);

        // Фон (тёмный)
        EditorGUI.DrawRect(area, new Color(0.18f, 0.18f, 0.18f, 1f));

        // Сетка
        DrawGrid();

        // Связи
        DrawLinks();

        // Ноды (GUI.Button — принимает клики)
        DrawNodes();

        // Тянущаяся линия
        DrawPendingLink();

        // Подсказка в углу
        DrawHints(area);

        if (dragging != null || panning)
            Repaint();
    }

    // =========================================================
    //  КООРДИНАТЫ
    //  world -> screen: screen = pan + world * zoom
    //  screen -> world: world = (screen - pan) / zoom
    // =========================================================
    private Rect NodeScreenRect(SkillDefinition s) =>
        new Rect(
            pan.x + s.TreePosition.x * zoom - (NodeW * zoom) * 0.5f,
            pan.y + s.TreePosition.y * zoom - (NodeH * zoom) * 0.5f,
            NodeW * zoom,
            NodeH * zoom);

    private Vector2 NodeScreenCenter(SkillDefinition s) =>
        new Vector2(
            pan.x + s.TreePosition.x * zoom,
            pan.y + s.TreePosition.y * zoom);

    private Vector2 ScreenToWorld(Vector2 screen) =>
        new Vector2(
            (screen.x - pan.x) / zoom,
            (screen.y - pan.y) / zoom);

    // =========================================================
    //  INPUT
    // =========================================================
    private void HandleInput()
    {
        var e = Event.current;
        if (e == null) return;

        // --- PAN: средняя кнопка ИЛИ Alt+ЛКМ ИЛИ ПКМ (если не LinkMode) ---
        bool panButton =
            e.button == 2 ||
            (e.button == 0 && e.alt) ||
            (e.button == 1 && !linkMode);

        if (e.type == EventType.MouseDown && panButton)
        {
            panning = true;
            panStartMouse = e.mousePosition;
            panStartValue = pan;
            e.Use();
            return;
        }

        if (e.type == EventType.MouseDrag && panning)
        {
            pan = panStartValue + (e.mousePosition - panStartMouse);
            e.Use();
            Repaint();
            return;
        }

        if (e.type == EventType.MouseUp && panning)
        {
            panning = false;
            e.Use();
            return;
        }

        // --- ЗУМ колесом вокруг курсора ---
        if (e.type == EventType.ScrollWheel && !panning)
        {
            Vector2 mouseBefore = ScreenToWorld(e.mousePosition);
            float newZoom = Mathf.Clamp(zoom - e.delta.y * 0.05f, 0.3f, 2f);
            if (!Mathf.Approximately(newZoom, zoom))
            {
                zoom = newZoom;
                // держим точку под курсором на месте
                pan.x = e.mousePosition.x - mouseBefore.x * zoom;
                pan.y = e.mousePosition.y - mouseBefore.y * zoom;
            }
            e.Use();
            Repaint();
            return;
        }

        // --- ПКМ в LinkMode — отмена ---
        if (linkMode && e.type == EventType.MouseDown && e.button == 1)
        {
            linkFrom = null;
            e.Use();
            Repaint();
            return;
        }

        // --- ЛКМ в LinkMode — назначение parent ---
        if (linkMode && e.type == EventType.MouseDown && e.button == 0)
        {
            var hit = NodeUnderMouse(e.mousePosition);
            if (hit != null)
            {
                if (linkFrom == null) linkFrom = hit;
                else if (linkFrom != hit)
                {
                    var so = new SerializedObject(hit);
                    so.FindProperty("parent").objectReferenceValue = linkFrom;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(hit);
                    linkFrom = null;
                }
                e.Use();
                Repaint();
            }
            return;
        }

        // --- Начало перетаскивания ноды ---
        if (!linkMode && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            var hit = NodeUnderMouse(e.mousePosition);
            if (hit != null)
            {
                dragging = hit;
                dragOffset = ScreenToWorld(e.mousePosition) - hit.TreePosition;
            }
            else dragging = null;
        }

        // --- Перетаскивание ноды ---
        if (e.type == EventType.MouseDrag && dragging != null && e.button == 0)
        {
            Vector2 newPos = ScreenToWorld(e.mousePosition) - dragOffset;

            if (!e.shift)
                newPos = new Vector2(
                    Mathf.Round(newPos.x / GridStep) * GridStep,
                    Mathf.Round(newPos.y / GridStep) * GridStep);

            var so = new SerializedObject(dragging);
            so.FindProperty("treePosition").vector2Value = newPos;
            so.ApplyModifiedPropertiesWithoutUndo();

            e.Use();
            Repaint();
        }

        // --- Отпускание ноды — финальный snap ---
        if (e.type == EventType.MouseUp && dragging != null && e.button == 0)
        {
            if (!e.shift)
            {
                var p = dragging.TreePosition;
                var snapped = new Vector2(
                    Mathf.Round(p.x / GridStep) * GridStep,
                    Mathf.Round(p.y / GridStep) * GridStep);
                var so = new SerializedObject(dragging);
                so.FindProperty("treePosition").vector2Value = snapped;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(dragging);
            dragging = null;
            e.Use();
            Repaint();
        }
    }

    private SkillDefinition NodeUnderMouse(Vector2 mouse)
    {
        for (int i = placed.Count - 1; i >= 0; i--)
        {
            var s = placed[i];
            if (s == null) continue;
            if (NodeScreenRect(s).Contains(mouse)) return s;
        }
        return null;
    }

    // =========================================================
    //  DRAW
    // =========================================================
    private void DrawGrid()
    {
        float stepPx = GridStep * zoom;

        // Начало сетки в screen-space: pan сдвинут на ближайшую кратную клетку
        float startX = Mathf.Repeat(pan.x, stepPx);
        float startY = Mathf.Repeat(pan.y, stepPx);

        for (float x = startX; x < position.width; x += stepPx)
            EditorGUI.DrawRect(new Rect(x, 0, 1f, position.height),
                               new Color(1, 1, 1, 0.07f));

        for (float y = startY; y < position.height; y += stepPx)
            EditorGUI.DrawRect(new Rect(0, y, position.width, 1f),
                               new Color(1, 1, 1, 0.07f));

        // Оси X/Y — чуть ярче
        if (pan.x >= 0 && pan.x <= position.width)
            EditorGUI.DrawRect(new Rect(pan.x, 0, 1f, position.height),
                               new Color(1, 1, 1, 0.2f));
        if (pan.y >= 0 && pan.y <= position.height)
            EditorGUI.DrawRect(new Rect(0, pan.y, position.width, 1f),
                               new Color(1, 1, 1, 0.2f));
    }

    private void DrawLinks()
    {
        foreach (var child in placed)
        {
            if (child == null || child.Parent == null) continue;
            if (!placed.Contains(child.Parent)) continue;

            Vector2 a = NodeScreenCenter(child.Parent);
            Vector2 b = NodeScreenCenter(child);

            Handles.BeginGUI();
            Handles.color = child.StartingSkill
                ? new Color(0.3f, 0.85f, 0.4f, 0.9f)
                : new Color(0.7f, 0.7f, 0.7f, 0.9f);
            Handles.DrawAAPolyLine(3f * zoom, a, b);
            Handles.EndGUI();
        }
    }

    private void DrawNodes()
    {
        foreach (var skill in placed)
        {
            if (skill == null) continue;

            var rect = NodeScreenRect(skill);

            Color bg = skill.StartingSkill ? new Color(0.3f, 0.85f, 0.4f)
                     : skill.Parent != null ? new Color(0.35f, 0.55f, 0.9f)
                     : new Color(0.6f, 0.6f, 0.6f);
            if (skill == linkFrom) bg = Color.cyan;

            var old = GUI.color;
            GUI.color = bg;
            GUI.Button(rect, GUIContent.none);
            GUI.color = old;

            if (skill.Icon != null)
            {
                var iconRect = new Rect(rect.x + 6, rect.y + 6,
                                        36 * zoom, 36 * zoom);
                GUI.DrawTexture(iconRect, skill.Icon.texture, ScaleMode.ScaleToFit);
            }

            if (skill.Frame != null)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), Color.white);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), Color.white);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), Color.white);
                EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), Color.white);
            }

            float textX = skill.Icon != null ? 48f * zoom : 6f;
            GUI.Label(new Rect(rect.x + textX, rect.y + 4,
                               rect.width - textX - 4, 18),
                      skill.Title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + textX, rect.y + 22,
                               rect.width - textX - 4, 16),
                      skill.Parent != null ? $"↑ {skill.Parent.Title}" : "root",
                      EditorStyles.miniLabel);
            GUI.Label(new Rect(rect.x + textX, rect.y + 38,
                               rect.width - textX - 4, 16),
                      $"cost {skill.KillCost}", EditorStyles.miniLabel);
        }
    }

    private void DrawPendingLink()
    {
        if (!linkMode || linkFrom == null) return;
        var e = Event.current;
        if (e == null) return;

        Handles.BeginGUI();
        Handles.color = Color.cyan;
        Handles.DrawAAPolyLine(3f, NodeScreenCenter(linkFrom), e.mousePosition);
        Handles.EndGUI();
    }

    private void DrawHints(Rect area)
    {
        var hint = new GUIContent(
            "СКМ/Alt+ЛКМ/ПКМ — двигать. Колесо — зум. Shift+drag ноды — без сетки. Toggle Link Mode — связи.");
        var style = EditorStyles.miniLabel;
        var size = style.CalcSize(hint);
        var r = new Rect(area.x + 8, area.yMax - size.y - 8, size.x, size.y);
        GUI.Label(r, hint, style);
    }

    // =========================================================
    //  TOOLBAR
    // =========================================================
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Save All", EditorStyles.toolbarButton))
            SaveAll();

        if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
        {
            RefreshAssets();
            Repaint();
        }

        bool newLinkMode = GUILayout.Toggle(linkMode, "Link Mode", EditorStyles.toolbarButton);
        if (newLinkMode != linkMode)
        {
            linkMode = newLinkMode;
            if (!linkMode) linkFrom = null;
        }

        zoom = EditorGUILayout.Slider(zoom, 0.3f, 2f, GUILayout.Width(180));

        if (GUILayout.Button("Center", EditorStyles.toolbarButton))
        {
            pan = new Vector2(position.width * 0.5f, position.height * 0.5f);
            Repaint();
        }

        if (GUILayout.Button("Focus All", EditorStyles.toolbarButton))
            FocusAllNodes();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Clear Parents", EditorStyles.toolbarButton))
        {
            foreach (var s in placed)
            {
                if (s == null) continue;
                var so = new SerializedObject(s);
                so.FindProperty("parent").objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(s);
            }
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Add:", GUILayout.Width(40));
        foreach (var s in allSkills)
        {
            if (s == null || placed.Contains(s)) continue;
            if (GUILayout.Button(s.Title, EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                placed.Add(s);
                var so = new SerializedObject(s);
                so.FindProperty("treePosition").vector2Value =
                    new Vector2(NextFreeColumn() * GridStep, 0);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(s);
                Repaint();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private int NextFreeColumn()
    {
        int col = 0;
        bool used = true;
        while (used)
        {
            used = false;
            foreach (var s in placed)
                if (Mathf.Approximately(s.TreePosition.x, col * GridStep))
                { used = true; col++; break; }
        }
        return col;
    }

    /// <summary>Подгоняет pan и zoom, чтобы все ноды были в кадре.</summary>
    private void FocusAllNodes()
    {
        if (placed.Count == 0)
        {
            pan = new Vector2(position.width * 0.5f, position.height * 0.5f);
            zoom = 1f;
            Repaint();
            return;
        }

        Vector2 min = placed[0].TreePosition;
        Vector2 max = placed[0].TreePosition;
        foreach (var s in placed)
        {
            min = Vector2.Min(min, s.TreePosition);
            max = Vector2.Max(max, s.TreePosition);
        }

        Vector2 center = (min + max) * 0.5f;
        float w = Mathf.Max(max.x - min.x + NodeW, 200f);
        float h = Mathf.Max(max.y - min.y + NodeH, 200f);

        float zoomX = position.width / w;
        float zoomY = position.height / h;
        zoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY) * 0.8f, 0.3f, 2f);

        pan.x = position.width * 0.5f - center.x * zoom;
        pan.y = position.height * 0.5f - center.y * zoom;

        Repaint();
    }

    private void SaveAll()
    {
        foreach (var s in placed)
            if (s != null) EditorUtility.SetDirty(s);
        AssetDatabase.SaveAssets();
        Debug.Log("Skill tree saved.");
    }
}
#endif