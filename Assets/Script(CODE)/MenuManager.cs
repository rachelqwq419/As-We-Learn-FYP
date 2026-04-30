using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI 面板")]
    public GameObject levelSelectPanel;

    [Header("Level 按鈕文字 (請順序拖入 Lv1 到 Lv6 嘅 Text)")]
    public TMP_Text[] levelButtonLabels;

    private string currentSubject;

    void Start()
    {
        currentSubject = PlayerPrefs.GetString("CurrentSubject", "Chinese");
        Debug.Log("載入科目區域: " + currentSubject);

        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(true);
        }

        string[] levelNames = { "Grade 1 Level", "Grade 2 Level", "Grade 3 Level", "Grade 4 Level", "Grade 5 Level", "Grade 6 Level" };

        if (levelButtonLabels != null)
        {
            for (int i = 0; i < levelButtonLabels.Length && i < levelNames.Length; i++)
            {
                if (levelButtonLabels[i] != null)
                {
                    levelButtonLabels[i].text = levelNames[i];
                }
            }
        }
    }

    public void SelectLevel(int level)
    {
        GameData.chosenSubject = currentSubject;
        GameData.chosenLevel = level;
        Debug.Log("最終決定: " + currentSubject + " - Lv." + level);
        SceneManager.LoadScene("BattleScene");
    }

    public void ClosePanel()
    {
        SceneManager.LoadScene("Map_MainCity"); 
        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(false);
        }
    }
}