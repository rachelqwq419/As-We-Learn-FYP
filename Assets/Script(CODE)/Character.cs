using System.Collections;
using System.Collections.Generic;
//using UnityEditor.U2D.Animation;
using UnityEngine;

public class Character : MonoBehaviour
{
    public CharacterData characterData;
    [Header("Equipment")]
    public ItemData equippedWeapon;
    public ItemData equippedArmor;
    public ItemData equippedBoots;
    public ItemData equippedHelm;

    public string characterName;
    public int level = 1;
    public int currentExp;
    public int expToNextLevel;
    public Sprite characterIcon;
    public int health;
    public int maxHealth;
    public int attackPower;
    public int defencePower;
    public int maxMana;
    public int mana;
    [Range(0f, 1f)] public float criticalChance = 0.1f; // 10% by default
    public float criticalMultiplier = 2f; // Double damage

    public bool isPoisoned;
    public int poisonDamage;
    public int poisonTurnsRemaining;

    public void InitializeFromData()
    {
        if (characterData == null) return;
        characterName = characterData.characterName;
        characterIcon = characterData.characterIcon;

        attackPower = Mathf.RoundToInt(GetTotalStat(StatsType.AD));
        defencePower = Mathf.RoundToInt(GetTotalStat(StatsType.DF));
        maxHealth = Mathf.RoundToInt(GetTotalStat(StatsType.HP));
        maxMana = Mathf.RoundToInt(GetTotalStat(StatsType.MP));
        criticalChance = GetTotalStat(StatsType.CR);
        criticalMultiplier = GetTotalStat(StatsType.CRD);
        poisonDamage = Mathf.RoundToInt(GetTotalStat(StatsType.PD));

    }

    public void Equip(ItemData item)
    {
        switch (item.type)
        {
            case ItemType.EquipableWeapon: equippedWeapon = item; break;
            case ItemType.EquipableArmor: equippedArmor = item; break;
            case ItemType.EquipableBoots: equippedBoots = item; break;
            case ItemType.EquipableHelm: equippedHelm = item; break;
        }
        RecalculateStats();
    }



    public ItemData Unequip(ItemType type)
    {
        ItemData unequipped = null;
        switch (type)
        {
            case ItemType.EquipableWeapon: unequipped = equippedWeapon; equippedWeapon = null; break;
            case ItemType.EquipableArmor: unequipped = equippedArmor; equippedArmor = null; break;
            case ItemType.EquipableBoots: unequipped = equippedBoots; equippedBoots = null; break;
            case ItemType.EquipableHelm: unequipped = equippedHelm; equippedHelm = null; break;
        }
        return unequipped;
    }

    public ItemData GetEquippedItem(ItemType type)
    {
        return type switch
        {
            ItemType.EquipableWeapon => equippedWeapon,
            ItemType.EquipableArmor => equippedArmor,
            ItemType.EquipableBoots => equippedBoots,
            ItemType.EquipableHelm => equippedHelm,
            _ => null
        };
    }

    public float GetTotalStat(StatsType type)
    {
        float baseValue = GetBaseStat(type);
        float bonus = 0f;

        bonus += GetItemBonus(equippedWeapon, type);
        bonus += GetItemBonus(equippedArmor, type);
        bonus += GetItemBonus(equippedBoots, type);
        bonus += GetItemBonus(equippedHelm, type);
        // save the  stats
        return baseValue + bonus;
    }

    private float GetItemBonus(ItemData item, StatsType type)
    {
        if (item == null || item.statsOfItem == null)
        {
            return 0f;
        }

        float bonus = 0f;
        foreach (var stat in item.statsOfItem)
        {
            if (stat.type == type)
            {
                bonus += stat.value;
                // Debug.Log($"Found bonus for {type}: {stat.value} from item {item.name}");
            }
        }
        return bonus;
    }

    private float GetBaseStat(StatsType type)
    {
        switch (type)
        {
            case StatsType.HP: return characterData.baseMaxHealth;
            case StatsType.MP: return characterData.baseMaxMana;
            case StatsType.AD: return characterData.baseAttackPower;
            case StatsType.DF: return characterData.baseDefencePower;
            case StatsType.CR: return characterData.BaseCriticalChance;
            case StatsType.CRD: return characterData.BaseCriticalMultiplier;
            case StatsType.PD: return characterData.BasePoisonDamage;
            case StatsType.XP: return characterData.currentExp;
            default: return 0f;
        }
    }

    public void RecalculateStats()
    {
        int oldMaxHealth = maxHealth; // 記低舊上限
        attackPower = Mathf.RoundToInt(GetTotalStat(StatsType.AD));
        defencePower = Mathf.RoundToInt(GetTotalStat(StatsType.DF));
        maxHealth = Mathf.RoundToInt(GetTotalStat(StatsType.HP));
        maxMana = Mathf.RoundToInt(GetTotalStat(StatsType.MP));
        criticalChance = GetTotalStat(StatsType.CR);
        criticalMultiplier = GetTotalStat(StatsType.CRD);
        if (maxHealth > oldMaxHealth)
        {
            health += (maxHealth - oldMaxHealth);
        }

        if (health > maxHealth) health = maxHealth;
    }
}




