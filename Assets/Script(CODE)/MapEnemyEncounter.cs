using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MapEnemyEncounter : MonoBehaviour
{
    [Header("怪獸設定")]
    public string Subject = "Math";
    public int Level = 1;

    [Header("UI 連結")]
    public TextMeshProUGUI nameLabel;

    private void Start()
    {
        EnemyData enemyData = GetComponent<EnemyData>();

        if (nameLabel != null)
        {
            string mName = (enemyData != null && !string.IsNullOrEmpty(enemyData.monsterName))
                           ? enemyData.monsterName
                           : "Monster";

            nameLabel.text = $"{Subject} Lv.{Level}\n{mName}";
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"撞到怪獸！進入戰鬥：{Subject} Lv.{Level}");

            GameData.chosenSubject = Subject;
            GameData.chosenLevel = Level;

            SceneManager.LoadScene("BattleScene");
        }
    }
}