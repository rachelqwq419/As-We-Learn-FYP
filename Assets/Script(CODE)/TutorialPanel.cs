using UnityEngine;
using TMP_Pro;
using UnityEngine.EventSystems;

/// <summary>
/// 教學面板控制器：管理遊戲操作說明的顯示，並支援點擊切換中英文語系。
/// </summary>
public class TutorialPanel : MonoBehaviour, IPointerClickHandler
{
    public static TutorialPanel Instance { get; private set; }

    [Header("UI 組件")]
    public GameObject tutorialPanelRoot;
    public TMP_Text instructionText;

    [Header("文本內容")]
    [TextArea(10, 30)] public string instructionsCN; // 中文文本
    [TextArea(10, 30)] public string instructionsEN; // 英文文本

    private bool isEnglish = false; // 當前語言狀態標記

    void Awake()
    {
        // 實作單例模式 (Singleton)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 若 Inspector 未預填文本，則加載預設的教學內容
        if (string.IsNullOrEmpty(instructionsCN))
        {
            instructionsCN = "<size=120%><b>[!] 冒險者指南 (How to Play) / <color=#FFD700><b>左鍵切換語言</b></color> </b></size>\n\n" +
                            "<size=110%><b>>基本操作</b></size>\n" +
                            "• <color=#00FFFF><b>W A S D</b></color>：移動角色\n" +
                            "• <color=#00FFFF><b>E</b></color>：與 NPC 互動 (進入商店 / 合成台)\n" +
                            "• <color=#00FFFF><b>I</b></color>：打開背包及更換裝備\n" +
                            "• <color=#00FFFF><b>Enter (回車鍵)</b></color>：確認輸入 / 登入遊戲\n" +
                            "• <color=#00FFFF><b>M</b></color>：清除舊存檔數據 (如遇 Bug 可重置)\n\n" +
                            "<size=110%><b>> 戰鬥與學科專精</b></size>\n" +
                            "普通攻擊對知識怪獸傷害極低，請善用「題目」答題攻擊！\n" +
                            "戰鬥中可<b>點擊下方角色頭像</b>實時換人，發揮學科專長：\n" +
                            "• <color=#FF9999><b>Mia</b></color> -> 擅長 <b>中文區</b> (傷害 1.2倍)\n" +
                            "• <color=#FFFF99><b>Bella</b></color> -> 擅長 <b>英文區</b> (傷害 1.2倍)\n" +
                            "• <color=#99CCFF><b>Kael</b></color> -> 擅長 <b>數學區</b> (傷害 1.2倍)\n\n" +
                            "<size=110%><b>> 成長與學習報告</b></size>\n" +
                            "• 打贏一場戰鬥可獲 <color=#FFD700><b>1~5金幣</b></color>。\n" +
                            "• 善用金幣買裝，或收集掉落物<b>合成裝備</b>提升戰力。\n" +
                            "• 隨時點擊畫面上方 <color=#FFaa00><b>Summary</b></color> 查看錯題報告！";
        }
        if (string.IsNullOrEmpty(instructionsEN))
        {
            instructionsEN = "<size=120%><b>[!] Adventurer's Guide (Guide)</b></size>\n\n" +
                            "<size=110%><b>> Basic Controls</b></size>\n" +
                            "• <color=#00FFFF><b>W A S D</b></color>: Move Character\n" +
                            "• <color=#00FFFF><b>E</b>: Interact with NPC (Shop / Crafting Table)\n" +
                            "• <color=#00FFFF><b>I</b>: Open Inventory & Change Equipment\n" +
                            "• <color=#00FFFF><b>Enter</b>: Confirm Input / Login\n" +
                            "• <color=#00FFFF><b>M</b>: Clear Save Data (Reset if you encounter bugs)\n\n" +
                            "<size=110%><b>> Battle & Subject Mastery</b></size>\n" +
                            "Normal attacks deal very low damage. Use \"Question\" to answer questions!\n" +
                            "<b>Click character portraits</b> during battle to switch and use specialties:\n" +
                            "• <color=#FF9999><b>Mia</b></color> -> Master of <b>Chinese Area</b> (1.2x Damage)\n" +
                            "• <color=#FFFF99><b>Bella</b></color> -> Master of <b>English Area</b> (1.2x Damage)\n" +
                            "• <color=#99CCFF><b>Kael</b></color> -> Master of <b>Math Area</b> (1.2x Damage)\n\n" +
                            "<size=110%><b>> Progression & Study Report</b></size>\n" +
                            "• Win a battle to earn <color=#FFD700><b>1~5 Gold</b></color>.\n" +
                            "• Use Gold to buy gear or collect drops to <b>Craft Equipment</b>.\n" +
                            "• Need to review? Click <color=#FFaa00><b>Summary</b></color> at the top to see your wrong answers!";
        }

        UpdateDisplay();

        // 初始化時預設隱藏面板
        if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(false);
    }

    /// <summary>
    /// 處理點擊事件：當玩家點擊教學面板時切換語系
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isEnglish = !isEnglish;
            UpdateDisplay();
            Debug.Log($"語系切換至: {(isEnglish ? "English" : "中文")}");
        }
    }

    /// <summary>
    /// 根據當前語系狀態更新 UI 文本內容
    /// </summary>
    void UpdateDisplay()
    {
        if (instructionText != null)
        {
            instructionText.text = isEnglish ? instructionsEN : instructionsCN;
        }
    }

    // 開啟教學面板並刷新顯示
    public void OpenTutorial()
    {
        if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(true);
        UpdateDisplay();
    }

    // 關閉教學面板
    public void CloseTutorial()
    {
        if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(false);
    }

    // 供外部腳本調用的靜態開啟方法
    public static void OpenTutorialStatic()
    {
        if (Instance != null) Instance.OpenTutorial();
    }
}