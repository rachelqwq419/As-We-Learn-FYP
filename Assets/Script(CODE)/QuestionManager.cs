using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text; // 用於處理 UTF-8 編碼，確保中文字符顯示正常

/// <summary>
/// 題庫管理員：負責從 Google Sheets (CSV) 或本地資源載入題目，並管理題目提取邏輯。
/// </summary>
public class QuestionManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string qText;
        public string option1;
        public string option2;
        public string option3;
        public int correctIdx;
    }

    // 各學科題庫列表
    public List<Question> chineseBank = new List<Question>();
    public List<Question> englishBank = new List<Question>();
    public List<Question> mathBank = new List<Question>();

    /// <summary> 紀錄本場戰鬥已出現過的數學題，避免重複；當題庫耗盡後將切換至程序化生成。 </summary>
    readonly HashSet<string> _mathBankKeysUsedThisBattle = new HashSet<string>();

    public static QuestionManager instance;

    [Header("雲端題庫連結 (Google Sheet CSV)")]
    public string chineseCSVUrl = "";
    public string englishCSVUrl = "";
    public string mathCSVUrl = "";

    void Awake()
    {
        instance = this;
        StartCoroutine(InitQuestionBanks());
    }

    /// <summary>
    /// 初始化所有學科題庫
    /// </summary>
    IEnumerator InitQuestionBanks()
    {
        yield return StartCoroutine(DownloadOrLoadCSV(chineseCSVUrl, "QuestionData_Chinese", chineseBank));
        yield return StartCoroutine(DownloadOrLoadCSV(englishCSVUrl, "QuestionData_English", englishBank));
        yield return StartCoroutine(DownloadOrLoadCSV(mathCSVUrl, "QuestionData_Math", mathBank));
        Debug.Log("所有題庫加載程序完成。");
    }

    /// <summary>
    /// 優先從雲端下載 CSV，若失敗則從本地 Resources 載入備份
    /// </summary>
    IEnumerator DownloadOrLoadCSV(string url, string localFileName, List<Question> targetList)
    {
        bool downloadSuccess = false;

        if (!string.IsNullOrEmpty(url))
        {
            // 在 URL 後方加入隨機數，避免獲取到舊的緩存資料
            string cacheBuster = url + "&t=" + Random.Range(0, 1000000);

            using (UnityWebRequest www = UnityWebRequest.Get(cacheBuster))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // 使用 UTF-8 編碼解碼，防止中文出現亂碼
                    string decodedText = Encoding.UTF8.GetString(www.downloadHandler.data);

                    if (!string.IsNullOrEmpty(decodedText) && decodedText.Contains(","))
                    {
                        ParseCSV(decodedText, targetList);
                        if (targetList.Count > 0)
                        {
                            Debug.Log($"雲端更新成功：{localFileName}，現有 {targetList.Count} 題");
                            downloadSuccess = true;
                        }
                    }
                }
            }
        }

        // 若雲端下載失敗或網址為空，則使用 Resources 下的本地數據
        if (!downloadSuccess)
        {
            TextAsset localData = Resources.Load<TextAsset>(localFileName);
            if (localData != null)
            {
                Debug.Log($"雲端下載失敗，載入本地備份：{localFileName}");
                ParseCSV(localData.text, targetList);
            }
        }
    }

    /// <summary>
    /// 解析 CSV 字串並根據當前遊戲難度篩選題目
    /// </summary>
    void ParseCSV(string csvText, List<Question> targetList)
    {
        targetList.Clear();
        // 兼容 Windows (CRLF) 與 Unix (LF) 的換行符
        string[] rows = csvText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (rows.Length <= 1) return;

        // 讀取首行標題，根據名稱映射索引
        string[] headerCols = rows[0].Trim().Split(',');
        int levelCol = FindHeaderIndex(headerCols, "Level", "level", "等級", "年級");
        int qCol = FindHeaderIndex(headerCols, "Question", "question", "題目", "題干", "qText");
        int aCol = FindHeaderIndex(headerCols, "OptionA", "optionA", "A", "選項A", "option1");
        int bCol = FindHeaderIndex(headerCols, "OptionB", "optionB", "B", "選項B", "option2");
        int cCol = FindHeaderIndex(headerCols, "OptionC", "optionC", "C", "選項C", "option3");
        int correctCol = FindHeaderIndex(headerCols, "Correct", "correct", "Answer", "answer", "正確答案", "correctIdx");

        // 若找不到對應標題，則使用預設索引
        if (qCol < 0) qCol = 1;
        if (aCol < 0) aCol = 2;
        if (bCol < 0) bCol = 3;
        if (cCol < 0) cCol = 4;
        if (correctCol < 0) correctCol = 5;

        for (int i = 1; i < rows.Length; i++)
        {
            string line = rows[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');

            // 難度等級過濾
            if (levelCol >= 0 && cols.Length > levelCol)
            {
                if (int.TryParse(cols[levelCol].Trim(), out int rowLevel))
                {
                    if (rowLevel != GameData.chosenLevel) continue;
                }
            }

            int maxIdxNeeded = Mathf.Max(qCol, aCol, bCol, cCol, correctCol);
            if (cols.Length > maxIdxNeeded)
            {
                Question q = new Question();
                // 執行 Trim() 移除不可見字符或多餘空格，確保後續匹配準確
                q.qText = cols[qCol].Trim();
                q.option1 = cols[aCol].Trim();
                q.option2 = cols[bCol].Trim();
                q.option3 = cols[cCol].Trim();
                int.TryParse(cols[correctCol].Trim(), out q.correctIdx);
                targetList.Add(q);
            }
        }
    }

    /// <summary>
    /// 獲取指定學科的題目，數學科具備特殊的重複檢查與程序化生成邏輯
    /// </summary>
    public Question GetRandomQuestion(string subject)
    {
        if (subject == "Math")
            return GetMathQuestionBankFirstThenProcedural();

        return GetRandomQuestionFromList(subject);
    }

    public void ResetMathQuestionUsageForBattle()
    {
        _mathBankKeysUsedThisBattle.Clear();
    }

    /// <summary>
    /// 生成題目唯一標識符，用於比對題目是否重複
    /// </summary>
    static string MathQuestionKey(Question q)
    {
        if (q == null) return "";
        string o3 = q.option3 ?? "";
        return $"{q.qText}|{q.option1}|{q.option2}|{o3}|{q.correctIdx}";
    }

    /// <summary>
    /// 優先從數學題庫中抽取未使用的題目，若全部用盡則改為動態生成
    /// </summary>
    Question GetMathQuestionBankFirstThenProcedural()
    {
        if (mathBank != null && mathBank.Count > 0)
        {
            List<Question> unused = new List<Question>();
            for (int i = 0; i < mathBank.Count; i++)
            {
                Question q = mathBank[i];
                if (q == null) continue;
                string key = MathQuestionKey(q);
                if (string.IsNullOrEmpty(key)) continue;
                if (!_mathBankKeysUsedThisBattle.Contains(key))
                    unused.Add(q);
            }

            if (unused.Count > 0)
            {
                Question pick = unused[Random.Range(0, unused.Count)];
                _mathBankKeysUsedThisBattle.Add(MathQuestionKey(pick));
                return pick;
            }
        }

        return GenerateProceduralMathQuestion();
    }

    /// <summary>
    /// 根據當前選擇等級動態生成簡單的四則運算題目
    /// </summary>
    Question GenerateProceduralMathQuestion()
    {
        int level = Mathf.Clamp(GameData.chosenLevel, 1, 3);
        int a, b;
        Question q = new Question();

        if (level == 1)
        {
            a = Random.Range(1, 10);
            b = Random.Range(1, 10);
            q.qText = $"{a} + {b} = ?";
            q.option1 = (a + b).ToString();
        }
        else if (level == 2)
        {
            a = Random.Range(10, 50);
            b = Random.Range(10, 50);
            q.qText = $"{a} + {b} = ?";
            q.option1 = (a + b).ToString();
        }
        else
        {
            a = Random.Range(2, 9);
            b = Random.Range(2, 9);
            q.qText = $"{a} x {b} = ?";
            q.option1 = (a * b).ToString();
        }

        // [INPUT] 標籤用於通知 UI 使用輸入框模式而非選項按鈕
        q.option2 = "[INPUT]";
        q.option3 = "";
        q.correctIdx = 1;
        return q;
    }

    /// <summary>
    /// 從指定的學科題庫中隨機抽取一題
    /// </summary>
    Question GetRandomQuestionFromList(string subject)
    {
        List<Question> targetList = null;
        if (subject == "Chinese") targetList = chineseBank;
        else if (subject == "English") targetList = englishBank;

        if (targetList != null && targetList.Count > 0)
            return targetList[Random.Range(0, targetList.Count)];
        return null;
    }

    /// <summary>
    /// 匹配標題欄位索引，不分大小寫並忽略引號
    /// </summary>
    int FindHeaderIndex(string[] headerCols, params string[] candidates)
    {
        if (headerCols == null) return -1;

        for (int i = 0; i < headerCols.Length; i++)
        {
            string h = (headerCols[i] ?? "").Trim().Trim('"');
            if (string.IsNullOrEmpty(h)) continue;

            foreach (string c in candidates)
            {
                if (string.Equals(h, c, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }
        return -1;
    }
}