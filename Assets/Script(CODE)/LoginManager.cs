using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.IO;

/// <summary>
/// 登錄管理員：處理用戶身份驗證、雲端名單同步以及本地 JSON 存檔的初始化。
/// </summary>
public class LoginManager : MonoBehaviour
{
    [Header("UI 組件綁定")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text feedbackText;

    [Header("遠端數據配置")]
    [Tooltip("Google Sheet 發佈為 CSV 的原始連結")]
    public string studentCsvUrl = "YOUR_GOOGLE_SHEET_CSV_LINK_HERE";

    // 存放從雲端同步的帳號與密碼對應表
    private Dictionary<string, string> validStudents = new Dictionary<string, string>();

    void Start()
    {
        // 紀錄本機存檔路徑，用於開發調試
        Debug.Log($"[System] Persistent Data Path: {Application.persistentDataPath}");

        // 初始化時執行雲端名單同步
        StartCoroutine(DownloadStudentList());
    }

    /// <summary>
    /// 退出應用程序，處理編輯器與發佈版本的執行狀態。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[System] Application Quitting...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 從指定的 URL 下載 CSV 名單並解析。
    /// </summary>
    IEnumerator DownloadStudentList()
    {
        feedbackText.text = "正在連接學校系統...";
        UnityWebRequest www = UnityWebRequest.Get(studentCsvUrl);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            ParseCSV(www.downloadHandler.text);
            feedbackText.text = "系統連接成功！請登入。";
            Debug.Log($"[LoginManager] 已從雲端加載 {validStudents.Count} 筆帳號資料。");
        }
        else
        {
            feedbackText.text = "網絡錯誤，無法獲取學生名單。";
            Debug.LogError($"CSV Download Error: {www.error}");
        }
    }

    /// <summary>
    /// 解析 CSV 字串，提取帳號與密碼欄位並存入字典。
    /// </summary>
    void ParseCSV(string csvData)
    {
        validStudents.Clear();
        // 處理跨平台換行符
        string[] rows = csvData.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 略過首行標題，從索引 1 開始處理數據
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');
            if (columns.Length >= 2)
            {
                string user = columns[0].Trim();
                string pass = columns[1].Trim();
                if (!string.IsNullOrEmpty(user))
                {
                    validStudents[user] = pass;
                }
            }
        }
    }

    /// <summary>
    /// 執行登錄驗證邏輯，包含老師管理通道與學生權限校對。
    /// </summary>
    public void Login()
    {
        string user = usernameInput.text.Trim();
        string pass = passwordInput.text.Trim();

        // 老師專用管理入口
        if (user.ToLower() == "teacher")
        {
            Debug.Log("Directing to Teacher Management Portal...");
            Application.OpenURL("https://fypweb.pages.dev/");
            feedbackText.text = "歡迎老師，正在打開管理後台...";
            return;
        }

        // 基本空值檢查
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            feedbackText.text = "請輸入帳號及密碼！";
            return;
        }

        // 驗證帳號是否存在於名單中且密碼匹配
        if (validStudents.ContainsKey(user) && validStudents[user] == pass)
        {
            feedbackText.text = "登入成功！載入中...";

            // 紀錄當前登錄用戶，供後續數據上傳模組使用
            PlayerPrefs.SetString("CurrentUser", usernameInput.text);
            PlayerPrefs.Save();

            // 進入存檔加載與場景跳轉流程
            LoadLocalProgress(user);
        }
        else
        {
            feedbackText.text = "登入失敗：帳號或密碼錯誤。";
        }
    }

    /// <summary>
    /// 檢查本地是否存在該用戶的存檔文件，若無則進行初始化創建。
    /// </summary>
    private void LoadLocalProgress(string username)
    {
        string fileName = username + "_save.json";
        string path = Path.Combine(Application.persistentDataPath, fileName);

        Debug.Log($"[SaveSystem] User: {username}, File Path: {path}");

        if (File.Exists(path))
        {
            Debug.Log($"[SaveSystem] 已偵測到 {username} 的既有存檔。");
            // 此處可擴展讀取 JSON 並解析至全局進度管理器的邏輯
        }
        else
        {
            Debug.Log($"[SaveSystem] 首次登錄，正在為 {username} 創建初始存檔文件。");
            CreateInitialSaveFile(username, path);
        }

        // 驗證完成，跳轉至遊戲主地圖
        UnityEngine.SceneManagement.SceneManager.LoadScene("Map_Start");
    }

    /// <summary>
    /// 將初始數據結構序列化為 JSON 並寫入硬碟。
    /// </summary>
    private void CreateInitialSaveFile(string username, string path)
    {
        UserData initialData = new UserData
        {
            studentName = username,
            lastLoginTime = System.DateTime.Now.ToString(),
            levelProgress = 1
        };

        string json = JsonUtility.ToJson(initialData, true);

        try
        {
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            Debug.Log("[SaveSystem] 初始 JSON 文件寫入成功。");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 文件寫入異常: {e.Message}");
        }
    }
}

/// <summary>
/// 存檔數據結構：定義需要持久化儲存的用戶進度欄位。
/// </summary>
[System.Serializable]
public class UserData
{
    public string studentName;
    public string lastLoginTime;
    public int levelProgress;
}