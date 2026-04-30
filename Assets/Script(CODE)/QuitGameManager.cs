using UnityEngine;
using TMPro;

/// <summary>
/// 退出遊戲管理員：負責處理遊戲結束流程、暫停邏輯以及根據錯題數據顯示學習建議。
/// </summary>
public class QuitGameManager : MonoBehaviour
{
    [Header("UI 面板組件")]
    public GameObject quitConfirmPanel; // 退出確認視窗面板
    public TextMeshProUGUI analysisText; // 用於顯示弱項分析結果的文本

    void Start()
    {
        // 啟動時預設隱藏確認視窗
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 開啟退出面板並暫停遊戲時間
    /// </summary>
    public void OpenQuitPanel()
    {
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(true);
            UpdateAnalysisText(); // 生成並更新學習狀況報告
            Time.timeScale = 0;   // 暫停遊戲邏輯
        }
    }

    /// <summary>
    /// 取消退出動作並恢復遊戲進行
    /// </summary>
    public void CancelQuit()
    {
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
            Time.timeScale = 1; // 恢復正常遊戲速度
        }
    }

    /// <summary>
    /// 確認退出流程：處理不同環境（Editor/Build）的程序關閉
    /// </summary>
    public void ConfirmQuit()
    {
        // 退出前先行恢復時間縮放，避免對其他靜態邏輯產生影響
        Time.timeScale = 1;

        Debug.Log("Application Quit.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 在編輯器環境下停止運行
#else
        Application.Quit(); // 在正式發布版本中關閉程序
#endif
    }

    /// <summary>
    /// 獲取 Summary 的錯題記錄並更新 UI 文本顯示
    /// </summary>
    private void UpdateAnalysisText()
    {
        if (analysisText == null || Summary.Instance == null) return;

        // 從錯題系統獲取表現最弱的學科資訊
        var weakestInfo = Summary.Instance.GetWeakestAreaInfo();

        if (weakestInfo.count == 0)
        {
            // 無錯題記錄時的文本反饋
            analysisText.text = "<b><size=120%><color=#55FF55>Excellent Progress!</color></size></b>\n\n" +
                               "Your performance is perfect with no wrong answers!\n\n" +
                               "<size=90%>Are you sure you want to end your study session?</size>";
        }
        else
        {
            // 存在錯題時，根據統計結果顯示對應學科的學習建議
            analysisText.text = "<b><size=125%><color=#FFFFFF>Learning Analysis</color></size></b>\n\n" +
                               $"It seems you struggled a bit in the \n<b><color=#FFCC00>\"{weakestInfo.weakest} Section\"</color></b>\n\n" +
                               $"<size=90%>Targeted practice is recommended to improve your mastery.\n\n" +
                               "<b>Are you sure you want to leave now?</b></size>";
        }
    }
}