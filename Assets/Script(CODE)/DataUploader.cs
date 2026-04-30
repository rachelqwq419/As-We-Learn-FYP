using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataUploader : MonoBehaviour
{
    public static DataUploader Instance;

    [Header("Google Form 設定")]
    public string formURL = "https://docs.google.com/forms/d/e/1FAIpQLSdLUeDH1ptAJmFn3p3XltuutvA1BqFU0uIpx7icJbKd7NlW_A/formResponse";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RecordWrongAnswer(string questionInfo, string wrongChoice)
    {
        string username = PlayerPrefs.GetString("CurrentUser", "UnknownUser");
        StartCoroutine(PostToGoogle(username, questionInfo, wrongChoice));
    }

    IEnumerator PostToGoogle(string username, string questionInfo, string wrongChoice)
    {
        WWWForm form = new WWWForm();

        form.AddField("entry.183570268", username);     // 學生帳號
        form.AddField("entry.1206120685", questionInfo); // 答錯題目
        form.AddField("entry.920396475", wrongChoice);  // 錯誤選項
        form.AddField("entry.136355791", GameData.chosenSubject);//科目名

        using (UnityWebRequest www = UnityWebRequest.Post(formURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("上傳失敗: " + www.error);
            }
            else
            {
                Debug.Log("錯題已成功上傳俾老師！");
            }
        }
    }
}