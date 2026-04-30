using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// 戰鬥界面管理器：負責操控戰鬥過程中的所有 UI 交互、面板切換及題目顯示邏輯。
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    [Header("UI 面板引用")]
    public GameObject mainMenu;      // 戰鬥主選單
    public GameObject skillMenu;     // 技能/學科選擇選單
    public GameObject answerMenu;    // 答題介面選單
    public GameObject questionPanel; // 題目顯示區域

    [Header("文字內容")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI btnTextA;
    public TextMeshProUGUI btnTextB;
    public TextMeshProUGUI btnTextC;

    [Header("控制組件")]
    public CanvasGroup actionPanelGroup; // 用於統一控制操作面板的透明度與交互性

    [Header("選項按鈕物件")]
    public GameObject btnObjectA;
    public GameObject btnObjectB;
    public GameObject btnObjectC;

    [Header("學科按鈕鎖定")]
    public GameObject btnSubjectChinese;
    public GameObject btnSubjectEnglish;
    public GameObject btnSubjectMath;

    [Header("填充題介面")]
    public GameObject inputPanel;
    public TMP_InputField answerInputField;
    private string currentTextInputAnswer = "";

    [Header("圖片題目組件")]
    public RawImage questionImage;

    // 儲存當前題目數據
    private int currentCorrectAnswer;
    private int currentAttributeID;
    private string currentQuestionText;
    private string currentQuestionDisplayText;
    private string currentOptionAText;
    private string currentOptionBText;
    private string currentOptionCText;

    void Start()
    {
        ResolveSkillSubjectButtons();
        ShowPanel("Main");

        // 自動為操作面板內的所有按鈕綁定通用點擊音效
        if (actionPanelGroup != null)
        {
            Button[] allButtons = actionPanelGroup.GetComponentsInChildren<Button>(true);
            foreach (Button btn in allButtons)
            {
                btn.onClick.AddListener(() => {
                    if (BattleController.instance != null)
                    {
                        AudioSource source = BattleController.instance.sfxSource;
                        AudioClip clickSound = BattleController.instance.sfxClick;
                        if (source != null && clickSound != null) source.PlayOneShot(clickSound);
                    }
                });
            }
        }
    }

    /// <summary>
    /// 設置操作面板的交互狀態，通常用於敵方回合或播放動畫時鎖定 UI
    /// </summary>
    public void SetInteractable(bool canTouch)
    {
        if (actionPanelGroup != null)
        {
            actionPanelGroup.interactable = canTouch;
            actionPanelGroup.alpha = canTouch ? 1f : 0.5f; // 禁用時降低透明度以提供視覺反饋
        }
    }

    public void OnClick_Iaido() { ShowPanel("Skill"); }
    public void OnClick_Back() { ShowPanel("Main"); }

    public void OnClick_Run()
    {
        if (BattleController.instance != null) BattleController.instance.PlayerRun();
    }

    /// <summary>
    /// 處理施放技能邏輯：從題庫獲取隨機題目，解析 [IMG] 標籤並配置答題 UI
    /// </summary>
    public void OnClick_CastSpell(string subject)
    {
        if (subject == "Chinese") currentAttributeID = 0;
        else if (subject == "English") currentAttributeID = 1;
        else if (subject == "Math") currentAttributeID = 2;

        var q = QuestionManager.instance.GetRandomQuestion(subject);
        if (q == null) return;

        if (questionImage != null) questionImage.gameObject.SetActive(false);

        // 解析題目中的圖片標籤並啟動異步下載
        string finalQuestionText = q.qText;
        if (finalQuestionText.Contains("[IMG]") && finalQuestionText.Contains("[/IMG]"))
        {
            int startIdx = finalQuestionText.IndexOf("[IMG]") + 5;
            int endIdx = finalQuestionText.IndexOf("[/IMG]");
            string url = finalQuestionText.Substring(startIdx, endIdx - startIdx);

            finalQuestionText = finalQuestionText.Replace($"[IMG]{url}[/IMG]", "").Trim();
            StartCoroutine(DownloadImageRoutine(url));
        }

        questionText.text = finalQuestionText;
        currentQuestionDisplayText = finalQuestionText;

        currentCorrectAnswer = q.correctIdx;
        currentQuestionText = q.qText;
        currentOptionAText = q.option1;
        currentOptionBText = q.option2;
        currentOptionCText = q.option3;

        // 初始化答題組件顯示狀態
        if (btnObjectA != null) btnObjectA.SetActive(true);
        if (btnObjectB != null) btnObjectB.SetActive(true);
        if (btnObjectC != null) btnObjectC.SetActive(true);
        if (inputPanel != null) inputPanel.SetActive(false);

        // 根據題目類型（填充題或選擇題）動態調整 UI 佈局
        if (q.option2 == "[INPUT]")
        {
            if (btnObjectA != null) btnObjectA.SetActive(false);
            if (btnObjectB != null) btnObjectB.SetActive(false);
            if (btnObjectC != null) btnObjectC.SetActive(false);

            if (inputPanel != null)
            {
                inputPanel.SetActive(true);
                if (answerInputField != null) answerInputField.text = "";
            }
            currentTextInputAnswer = q.option1;
        }
        else if (string.IsNullOrEmpty(q.option3))
        {
            btnTextA.text = q.option1;
            btnTextB.text = q.option2;
            if (btnObjectC != null) btnObjectC.SetActive(false);
        }
        else
        {
            btnTextA.text = q.option1;
            btnTextB.text = q.option2;
            btnTextC.text = q.option3;
        }

        ShowPanel("Answer");
    }

    /// <summary>
    /// 格式化正確答案文本，用於錯題摘要報告
    /// </summary>
    string GetCorrectAnswerTextForSummary()
    {
        if (currentOptionBText == "[INPUT]")
            return currentTextInputAnswer ?? "";

        string PickMcqText()
        {
            if (string.IsNullOrEmpty(currentOptionCText))
            {
                if (currentCorrectAnswer == 1) return currentOptionAText ?? "";
                if (currentCorrectAnswer == 2) return currentOptionBText ?? "";
                return "";
            }

            if (currentCorrectAnswer == 1) return currentOptionAText ?? "";
            if (currentCorrectAnswer == 2) return currentOptionBText ?? "";
            if (currentCorrectAnswer == 3) return currentOptionCText ?? "";
            return "";
        }

        string ans = PickMcqText();
        string qStrip = StripImgTagsForCompare(currentQuestionDisplayText);
        string aStrip = StripImgTagsForCompare(ans);

        // 若答案文本與題目相同（如圖片對應圖片），則返還選項字母代號
        if (!string.IsNullOrEmpty(aStrip) && string.Equals(qStrip, aStrip, StringComparison.Ordinal))
        {
            int idx = Mathf.Clamp(currentCorrectAnswer, 1, 3);
            char letter = (char)('A' + idx - 1);
            return $"（正解：選項 {letter}）";
        }

        return ans;
    }

    /// <summary>
    /// 清除字符串中的 [IMG] 標籤，僅保留純文本進行比對
    /// </summary>
    static string StripImgTagsForCompare(string s)
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

    /// <summary>
    /// 異步下載題目關聯圖片並更新至 RawImage 組件
    /// </summary>
    IEnumerator DownloadImageRoutine(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                if (questionImage != null)
                {
                    questionImage.texture = texture;
                    questionImage.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("圖片資源下載失敗: " + www.error);
            }
        }
    }

    /// <summary>
    /// 處理選擇題點擊事件，驗證答案並記錄錯題數據
    /// </summary>
    public void OnClick_Answer(int optionIndex)
    {
        ShowPanel("Main");
        bool isCorrect = (optionIndex == currentCorrectAnswer);

        if (!isCorrect && DataUploader.Instance != null)
        {
            string wrongAnswerText = "";
            if (optionIndex == 1) wrongAnswerText = currentOptionAText;
            else if (optionIndex == 2) wrongAnswerText = currentOptionBText;
            else if (optionIndex == 3) wrongAnswerText = currentOptionCText;

            // 同步數據至雲端數據庫與本地摘要系統
            DataUploader.Instance.RecordWrongAnswer(currentQuestionDisplayText, wrongAnswerText);
            Summary.RecordWrong(
                GameData.chosenLevel,
                currentQuestionDisplayText,
                GetCorrectAnswerTextForSummary(),
                Summary.AreaLabelFromSubject(GameData.chosenSubject));
        }

        if (BattleController.instance != null)
        {
            BattleController.instance.PlayerAttack_Magic(isCorrect, currentAttributeID);
        }
    }

    /// <summary>
    /// 處理填充題提交事件
    /// </summary>
    public void OnClick_SubmitInput()
    {
        ShowPanel("Main");

        string userAnswer = "";
        if (answerInputField != null) userAnswer = answerInputField.text.Trim();

        bool isCorrect = string.Equals(userAnswer, currentTextInputAnswer, System.StringComparison.OrdinalIgnoreCase);

        if (!isCorrect && DataUploader.Instance != null)
        {
            DataUploader.Instance.RecordWrongAnswer(currentQuestionDisplayText, "玩家輸入: " + userAnswer);
            Summary.RecordWrong(
                GameData.chosenLevel,
                currentQuestionDisplayText,
                GetCorrectAnswerTextForSummary(),
                Summary.AreaLabelFromSubject(GameData.chosenSubject));
        }

        if (inputPanel != null) inputPanel.SetActive(false);

        if (BattleController.instance != null)
        {
            BattleController.instance.PlayerAttack_Magic(isCorrect, currentAttributeID);
        }
    }

    /// <summary>
    /// 初始化並自動連結技能菜單下的學科按鈕組件
    /// </summary>
    void ResolveSkillSubjectButtons()
    {
        if (skillMenu == null) return;
        foreach (Transform t in skillMenu.GetComponentsInChildren<Transform>(true))
        {
            if (btnSubjectChinese == null && t.name == "Btn_Chinese") btnSubjectChinese = t.gameObject;
            else if (btnSubjectEnglish == null && t.name == "Btn_English") btnSubjectEnglish = t.gameObject;
            else if (btnSubjectMath == null && t.name == "Btn_Math") btnSubjectMath = t.gameObject;
        }
    }

    /// <summary>
    /// 根據敵人的學科屬性鎖定特定的答題按鈕，實施學科相剋或特定區域限制
    /// </summary>
    void ApplyMonsterSubjectAreaLock()
    {
        if (BattleController.instance == null) return;

        EnemyData enemy = BattleController.instance.currentEnemy;
        if (enemy != null)
        {
            int monsterAttr = enemy.attribute; // 0: 中文, 1: 英文, 2: 數學
            if (btnSubjectChinese != null) btnSubjectChinese.SetActive(monsterAttr == 0);
            if (btnSubjectEnglish != null) btnSubjectEnglish.SetActive(monsterAttr == 1);
            if (btnSubjectMath != null) btnSubjectMath.SetActive(monsterAttr == 2);
        }
        else
        {
            if (btnSubjectChinese != null) btnSubjectChinese.SetActive(true);
            if (btnSubjectEnglish != null) btnSubjectEnglish.SetActive(true);
            if (btnSubjectMath != null) btnSubjectMath.SetActive(true);
        }
    }

    /// <summary>
    /// 切換面板顯示狀態並根據上下文執行必要的組件初始化
    /// </summary>
    void ShowPanel(string panelName)
    {
        mainMenu.SetActive(panelName == "Main");
        skillMenu.SetActive(panelName == "Skill");

        if (panelName == "Main" || panelName == "Skill")
            ApplyMonsterSubjectAreaLock();

        answerMenu.SetActive(panelName == "Answer");
        questionPanel.SetActive(panelName == "Answer");

        if (panelName != "Answer" && inputPanel != null) inputPanel.SetActive(false);
    }

    /// <summary>
    /// 角色切換點擊處理，僅在玩家回合內有效
    /// </summary>
    public void OnClick_SwitchCharacter(int teamIndex)
    {
        if (BattleController.instance != null && BattleController.instance.state == BattleState.PLAYER_TURN)
        {
            BattleController.instance.SwitchCharacter(teamIndex);
        }
    }
}