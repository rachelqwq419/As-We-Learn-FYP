using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 錯題摘要系統：負責記錄、持久化儲存以及顯示玩家答錯的題目。
/// 支援 JSON 檔案存取與 UI 列表動態生成。
/// </summary>
public class Summary : MonoBehaviour
{
    public static Summary Instance { get; private set; }

    [Serializable]
    public class WrongAnswerEntry
    {
        public int level;
        public string question;
        public string correctAnswer;
        public string area;
    }

    [Serializable]
    class WrongAnswerListFile
    {
        public List<WrongAnswerEntry> items = new List<WrongAnswerEntry>();
    }

    [Header("UI 核心組件")]
    [Tooltip("摘要面板的根物件 (例如 SummaryRoot)")]
    public GameObject summaryPanelRoot;

    [Tooltip("Scroll View 的內容容器 (Content)，用於生成錯題行")]
    public RectTransform contentRoot;

    [Tooltip("錯題行的 UI 模板。子物件需包含 level, question, correctAnswer, area 等命名的 TMP 組件")]
    public GameObject rowTemplate;

    [Header("顯示設定")]
    public bool startHidden = true;        // 啟動時是否預設隱藏
    public bool persistToDisk = true;      // 是否將資料儲存至硬碟
    public float rowHeight = 88f;          // 每一行的高度

    [Tooltip("自動為 Content 加上 VerticalLayoutGroup 與 ContentSizeFitter")]
    public bool setupContentLayout = true;

    [Tooltip("生成行時是否隱藏模板內的標籤 (Title)，避免每行重複顯示標題")]
    public bool hideTitleLabelsInClonedRows = true;

    // 儲存當前會話的錯題記錄
    static readonly List<WrongAnswerEntry> SessionEntries = new List<WrongAnswerEntry>();
    const string FileName = "wrong_answer_summary.json";

    // 存檔路徑指向裝置的持久化資料夾
    static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (persistToDisk) LoadFromDisk();
        RefreshUI();
    }

    void Start()
    {
        if (startHidden && summaryPanelRoot != null)
            summaryPanelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 分析當前錯題數據，找出答錯次數最多的學科領域。
    /// 供 QuitGameManager 等統計模組調用。
    /// </summary>
    public (string weakest, int count) GetWeakestAreaInfo()
    {
        if (SessionEntries.Count == 0) return ("None", 0);

        int chinese = 0, english = 0, math = 0;
        foreach (var e in SessionEntries)
        {
            if (e.area.Contains("Chinese")) chinese++;
            else if (e.area.Contains("English")) english++;
            else if (e.area.Contains("Math")) math++;
        }

        string weakest = "Chinese";
        int max = chinese;

        if (english > max) { weakest = "English"; max = english; }
        if (math > max) { weakest = "Math"; max = math; }

        return (weakest, max);
    }

    public void OpenSummaryPanel()
    {
        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(true);
        RefreshUI();
    }

    public static void OpenSummaryPanelStatic()
    {
        if (Instance != null) Instance.OpenSummaryPanel();
    }

    public void CloseSummaryPanel()
    {
        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(false);
    }

    public static void CloseSummaryPanelStatic()
    {
        if (Instance != null) Instance.CloseSummaryPanel();
    }

    /// <summary>
    /// 記錄一筆新的錯題，並視需求同步至硬碟與更新 UI。
    /// </summary>
    public static void RecordWrong(int level, string question, string correctAnswer, string area)
    {
        if (string.IsNullOrEmpty(question)) question = "(empty)";

        SessionEntries.Add(new WrongAnswerEntry
        {
            level = level,
            question = question,
            correctAnswer = correctAnswer ?? "",
            area = area ?? ""
        });

        bool save = Instance == null || Instance.persistToDisk;
        if (save) SaveToDisk();

        if (Instance != null) Instance.RefreshUI();
    }

    /// <summary>
    /// 將原始學科名稱轉換為易讀的區域標籤。
    /// </summary>
    public static string AreaLabelFromSubject(string subject)
    {
        if (string.IsNullOrEmpty(subject)) return "-";
        switch (subject)
        {
            case "Chinese": return "Chinese Area";
            case "English": return "English Area";
            case "Math": return "Math Area";
            default: return subject;
        }
    }

    /// <summary>
    /// 清除所有記憶體與硬碟中的錯題記錄。
    /// </summary>
    public void ClearAll()
    {
        SessionEntries.Clear();
        if (persistToDisk && File.Exists(SavePath)) File.Delete(SavePath);
        RefreshUI();
    }

    /// <summary>
    /// 從持久化儲存讀取 JSON 數據。
    /// </summary>
    void LoadFromDisk()
    {
        SessionEntries.Clear();
        if (!File.Exists(SavePath)) return;
        try
        {
            var data = JsonUtility.FromJson<WrongAnswerListFile>(File.ReadAllText(SavePath, System.Text.Encoding.UTF8));
            if (data?.items != null) SessionEntries.AddRange(data.items);
        }
        catch (Exception e) { Debug.LogWarning("[Summary] Load Error: " + e.Message); }
    }

    /// <summary>
    /// 將當前會話記錄序列化並寫入硬碟。
    /// </summary>
    static void SaveToDisk()
    {
        try
        {
            var json = JsonUtility.ToJson(new WrongAnswerListFile { items = new List<WrongAnswerEntry>(SessionEntries) }, true);
            File.WriteAllText(SavePath, json, System.Text.Encoding.UTF8);
        }
        catch (Exception e) { Debug.LogWarning("[Summary] Save Error: " + e.Message); }
    }

    void Update()
    {
        // 快捷鍵：在摘要畫面按下 Backspace 可快速清空所有歷史記錄
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            ClearAll();
            Debug.Log("Summary data has been fully cleared.");
        }
    }

    /// <summary>
    /// 重繪摘要 UI 列表。
    /// 清除舊有的物件並根據當前記錄重新生成每一行。
    /// </summary>
    public void RefreshUI()
    {
        if (rowTemplate == null || contentRoot == null)
        {
            Debug.LogWarning("[Summary] 請在 Inspector 設定 Content Root 與 Row Template。");
            return;
        }

        if (setupContentLayout)
            SetupVerticalList(contentRoot);

        // 清理現有的錯題行（保留模板本身）
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform c = contentRoot.GetChild(i);
            if (c.gameObject == rowTemplate) continue;
            if (c.name.StartsWith("SummaryRow_")) Destroy(c.gameObject);
        }

        rowTemplate.SetActive(false);

        // 倒序遍歷，讓最新的錯誤顯示在列表頂端
        for (int i = SessionEntries.Count - 1; i >= 0; i--)
        {
            GameObject row = Instantiate(rowTemplate, contentRoot);
            row.name = "SummaryRow_" + i;
            row.SetActive(true);

            if (hideTitleLabelsInClonedRows)
                HideTitles(row.transform);

            ApplyRowSize(row);
            FillRow(row.transform, SessionEntries[i]);
        }
    }

    /// <summary>
    /// 動態配置自動佈局組件，確保列表能隨內容成長。
    /// </summary>
    static void SetupVerticalList(RectTransform content)
    {
        var v = content.GetComponent<VerticalLayoutGroup>();
        if (v == null) v = content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperLeft;
        v.spacing = 8f;
        v.padding = new RectOffset(12, 12, 12, 12);
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var f = content.GetComponent<ContentSizeFitter>();
        if (f == null) f = content.gameObject.AddComponent<ContentSizeFitter>();
        f.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
    }

    void ApplyRowSize(GameObject row)
    {
        var rt = row.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        var le = row.GetComponent<LayoutElement>();
        if (le == null) le = row.AddComponent<LayoutElement>();
        le.preferredHeight = Mathf.Max(40f, rowHeight);
        le.minHeight = 40f;
        le.flexibleHeight = 0f;
    }

    /// <summary>
    /// 隱藏副本中的標題物件，避免 UI 過於雜亂。
    /// </summary>
    static void HideTitles(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root) continue;
            if (t.name.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
                t.gameObject.SetActive(false);
        }

        // 額外處理重複命名的 area 物件
        var areas = new List<Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == "area") areas.Add(t);
        if (areas.Count >= 2) areas[0].gameObject.SetActive(false);
    }

    /// <summary>
    /// 將單筆錯題資料填入 UI 行。
    /// </summary>
    static void FillRow(Transform row, WrongAnswerEntry e)
    {
        SetTmp(row, "level", e.level.ToString());
        SetTmp(row, "question", StripImg(e.question));
        SetTmp(row, new[] { "correctAnswer", "correct answer", "coreect answer", "coreect" }, StripImg(e.correctAnswer));
        SetTmpLastNamed(row, "area", e.area);
    }

    static void SetTmp(Transform root, string childName, string text)
    {
        var t = FindChildDeep(root, childName);
        if (t == null) return;
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = text ?? "";
    }

    static void SetTmp(Transform root, string[] names, string text)
    {
        foreach (var n in names)
        {
            var t = FindChildDeep(root, n);
            if (t != null)
            {
                var tmp = t.GetComponent<TMP_Text>();
                if (tmp != null) { tmp.text = text ?? ""; return; }
            }
        }
    }

    static void SetTmpLastNamed(Transform root, string name, string text)
    {
        TMP_Text last = null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != name) continue;
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null) last = tmp;
        }
        if (last != null) last.text = text ?? "";
    }

    static Transform FindChildDeep(Transform p, string name)
    {
        if (p.name == name) return p;
        for (int i = 0; i < p.childCount; i++)
        {
            var r = FindChildDeep(p.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    /// <summary>
    /// 移除文本中的自定義圖層標籤 [IMG]...[/IMG]，僅保留文字內容。
    /// </summary>
    static string StripImg(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        while (true)
        {
            int a = s.IndexOf("[IMG]", StringComparison.Ordinal);
            int b = s.IndexOf("[/IMG]", StringComparison.Ordinal);
            if (a < 0 || b < 0 || b < a) break;
            s = s.Remove(a, b - a + 7);
        }
        return s.Trim();
    }
}