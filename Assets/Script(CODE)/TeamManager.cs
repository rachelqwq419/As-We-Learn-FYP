using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 隊伍管理員：負責維護玩家隊伍成員、處理角色初始化以及管理單例狀態。
/// </summary>
public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance;

    [Header("隊伍設定")]
    public List<Character> playerTeamCharacters = new List<Character>(); // 當前隊伍成員名單

    public int maxTeamSize = 5;

    private void Awake()
    {
        SetupSingleton();
    }

    private void Start()
    {
        CollectTeamMembersFromScene();
    }

    /// <summary>
    /// 設定單例模式，確保 TeamManager 在切換場景時不會被銷毀。
    /// </summary>
    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 自動掃描並收集場景中的 Character 組件，並進行初步初始化。
    /// </summary>
    private void CollectTeamMembersFromScene()
    {
        playerTeamCharacters.Clear();
        Character[] allCharacters = FindObjectsOfType<Character>(true);

        foreach (var character in allCharacters)
        {
            if (character.characterData != null)
            {
                character.InitializeFromData();

                // 初始化角色狀態：補滿血量，確保從地圖進入戰鬥時處於健康狀態
                character.health = character.maxHealth;

                if (character.characterIcon == null && character.characterData != null)
                    character.characterIcon = character.characterData.characterIcon;

                // 將隊員設為子物件並暫時隱藏
                character.transform.SetParent(transform);
                character.gameObject.SetActive(false);
                playerTeamCharacters.Add(character);
            }
        }

        Debug.Log($"TeamManager 已完成收集，當前隊伍人數：{playerTeamCharacters.Count}");
    }

    /// <summary>
    /// 透過 Prefab 動態生成新成員並加入隊伍。
    /// </summary>
    public bool AddToTeam(GameObject characterPrefab)
    {
        if (playerTeamCharacters.Count >= maxTeamSize)
        {
            Debug.LogWarning("隊伍已滿，無法加入新隊友。");
            return false;
        }

        GameObject newMember = Instantiate(characterPrefab);
        Character newCharacter = newMember.GetComponent<Character>();

        if (newCharacter == null)
        {
            Debug.LogError("傳入的 Prefab 缺少 Character 元件：" + characterPrefab.name);
            Destroy(newMember);
            return false;
        }

        newCharacter.InitializeFromData();

        // 確保新加入的成員血量為滿狀態
        newCharacter.health = newCharacter.maxHealth;

        if (newCharacter.characterIcon == null && newCharacter.characterData != null)
            newCharacter.characterIcon = newCharacter.characterData.characterIcon;

        playerTeamCharacters.Add(newCharacter);
        newMember.SetActive(false);

        Debug.Log($"成功加入隊友：{newCharacter.characterName}");

        return true;
    }

    /// <summary>
    /// 從隊伍名單中移除指定角色並銷毀其實體。
    /// </summary>
    public void RemoveFromTeam(Character characterToRemove)
    {
        if (playerTeamCharacters.Remove(characterToRemove))
        {
            if (characterToRemove != null && characterToRemove.gameObject != null)
            {
                Destroy(characterToRemove.gameObject);
            }
            Debug.Log($"已將 {characterToRemove?.characterName} 從隊伍中移除。");
        }
    }
}