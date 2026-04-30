using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleLauncher : MonoBehaviour
{
    [Header("環境設定")]
    public SpriteRenderer backgroundRenderer; // 背景顯示器
    public Transform enemyStation;            // 怪物生成的站位點

    [Header("背景圖片")]
    public Sprite chineseBG;
    public Sprite englishBG;
    public Sprite mathBG;

    [Header("怪物預製物 (請依照 Lv1 ~ Lv5 順序拖進來)")]
    public GameObject[] chineseMonsters; // 放5隻中文怪
    public GameObject[] englishMonsters; // 放5隻英文怪
    public GameObject[] mathMonsters;    // 放5隻數學怪

    void Start()
    {
        // 1. 讀取玩家選擇
        string subject = GameData.chosenSubject;
        int level = GameData.chosenLevel; // 1~5

        if (string.IsNullOrEmpty(subject)) subject = "Chinese";
        if (level < 1) level = 1;

        Debug.Log($"戰鬥開始！科目: {subject}, 等級: {level}");


        // 2. 設定背景
        if (subject == "Chinese") backgroundRenderer.sprite = chineseBG;
        else if (subject == "English") backgroundRenderer.sprite = englishBG;
        else if (subject == "Math") backgroundRenderer.sprite = mathBG;

        // 3. 生成怪物
        SpawnEnemy(subject, level);
    }

    void SpawnEnemy(string subject, int level)
    {
        int index = level - 1; 
        GameObject prefabToSpawn = null;

        // 根據科目去對應的清單找怪物
        if (subject == "Chinese")
        {
            if (index < chineseMonsters.Length) prefabToSpawn = chineseMonsters[index];
        }
        else if (subject == "English")
        {
            if (index < englishMonsters.Length) prefabToSpawn = englishMonsters[index];
        }
        else if (subject == "Math")
        {
            if (index < mathMonsters.Length) prefabToSpawn = mathMonsters[index];
        }

        // 4. 實體化 (Instantiate)
        if (prefabToSpawn != null)
        {
            // 在 EnemyStation 的位置生成怪物
            Instantiate(prefabToSpawn, enemyStation.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("出錯了！找不到對應的怪物檔案，請檢查 Inspector 有沒有拉好 5 隻怪。");
        }
    }
}