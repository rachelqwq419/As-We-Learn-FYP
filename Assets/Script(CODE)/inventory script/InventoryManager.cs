using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{

    public static InventoryManager instance;
    public PlayerController player;
    [Header("Inventory Panel")]
    public GameObject inventoryPanel;
    public ItemSlotUI[] uiSlots;
    public ItemSlot[] slots;

    [Header("Character List UI")]

    public TextMeshProUGUI currentCharacterText;

    [Header("Selected Item Info")]
    private ItemSlot selectedItem;
    private int selectedItemIndex;
    public TextMeshProUGUI selectedItemName, selectedItemDescription, selectedItemstatName, selectedItemsStatValue;
    public GameObject useButton, equipButton, unequipButton;

    public List<Character> activeTeam = new List<Character>();
    private Character selectedCharacter;

    public TextMeshProUGUI goldText;

    [Header("Buttons (Fixed Team Slots)")]
    public Button[] teamButtons = new Button[5];

    [Header("UI Display")]
    public TextMeshProUGUI statsText;
    private int lastSelectedCharacterIndex = -1;


    private const int WeaponSlotIndex = 0;
    private const int HelmSlotIndex = 1;
    private const int ArmorSlotIndex = 2;
    private const int BootsSlotIndex = 3;
    private const int ReservedSlotCount = 4;
    private const int GeneralInventoryStartIndex = ReservedSlotCount;
    // Start is called before the first frame update

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        SetupButtons();

        slots = new ItemSlot[uiSlots.Length];
        for (int x = 0; x < slots.Length; x++)
        {
            slots[x] = new ItemSlot();
            uiSlots[x].index = x;
            uiSlots[x].Clear();
        }

        SaveManager.instance.Load();
        ClearSelectedItemWindow();
        UpdateUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    private void ShowCharacterStats(int index)
    {
        if (TeamManager.Instance == null ||
            index >= TeamManager.Instance.playerTeamCharacters.Count ||
            index < 0)
        {
            statsText.text = $"Slot {index + 1} is empty or TeamManager not found.";
            selectedCharacter = null;
            lastSelectedCharacterIndex = -1;
            UpdateUI();
            return;
        }

        Character character = TeamManager.Instance.playerTeamCharacters[index];

        if (character == null || character.characterData == null)
        {
            statsText.text = $"Character data missing in slot {index + 1}.";
            selectedCharacter = null;
            lastSelectedCharacterIndex = -1;
            UpdateUI();
            return;
        }

        selectedCharacter = character;
        lastSelectedCharacterIndex = index;

        string stats = $"<b>{character.characterName}</b>\n" +
                       $"Level: {character.level}\n" +
                       $"HP: {character.maxHealth}\n" +
                       $"MP: {character.maxMana}\n" +
                       $"AD: {character.attackPower}\n" +
                       $"DEF: {character.defencePower}\n" +
                       $"CR: {character.criticalChance * 100f}%\n" +
                       $"CRD: x{character.criticalMultiplier}\n" +
                       $"PD: {character.poisonDamage}";

        statsText.text = stats;

        UpdateUI();
    }

    private void SetupButtons()
    {
        if (TeamManager.Instance == null)
        {
            Debug.LogError("TeamManager.Instance 不存在！");
            return;
        }

        for (int i = 0; i < teamButtons.Length; i++)
        {
            if (teamButtons[i] == null) continue;

            int index = i;
            teamButtons[i].onClick.RemoveAllListeners();
            teamButtons[i].onClick.AddListener(() => ShowCharacterStats(index));

            if (index < TeamManager.Instance.playerTeamCharacters.Count)
            {
                Character character = TeamManager.Instance.playerTeamCharacters[index];

                if (character != null && character.characterIcon != null)
                {
                    Image image = teamButtons[i].GetComponent<Image>();
                    if (image != null)
                    {
                        image.sprite = character.characterIcon;
                        image.color = Color.white;
                    }
                }

                teamButtons[i].gameObject.SetActive(true);
            }
            else
            {
                teamButtons[i].gameObject.SetActive(false);
            }
        }
    }

    ItemSlot GetEmptySlot()
    {
        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)
        {
            if (slots[i].item == null)
                return slots[i];
        }
        return null;
    }

    ItemSlot GetItemStack(ItemData item)
    {
        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)
        {
            if (slots[i].item == item && slots[i].quantity < item.maxStackAmount)
                return slots[i];
        }
        return null;
    }
    public void AddItem(ItemData item)
    {
        if (item.canStack)
        {
            ItemSlot stackSlot = GetItemStack(item);
            if (stackSlot != null)
            {
                stackSlot.quantity++;
               UpdateUI();
               SaveManager.instance.SaveInventory();
                return;
            }
        }

        ItemSlot emptySlot = GetEmptySlot();
        if (emptySlot != null)
        {
            emptySlot.item = item;
            emptySlot.quantity = 1;
            UpdateUI();
            SaveManager.instance.SaveInventory();
            return;
        }
        ThrowItem(item);
        SaveManager.instance.SaveInventory();
    }
    public void ThrowItem(ItemData item)
    {
        // 加入安全檢查
        if (item.dropPrefab != null && player != null)
        {
            Instantiate(item.dropPrefab, player.transform.position, player.transform.rotation);
        }
        else
        {
            Debug.LogWarning("爆背包！但物品沒有設定掉落物模型，或找不到玩家！");
        }
    }

    public void UpdateUI()
    {
        goldText.text = GoldManager.instance.gold.ToString();
        if (selectedCharacter != null)
        {
            slots[WeaponSlotIndex].item = selectedCharacter.equippedWeapon;
            slots[WeaponSlotIndex].quantity = selectedCharacter.equippedWeapon != null ? 1 : 0;

            slots[ArmorSlotIndex].item = selectedCharacter.equippedArmor;
            slots[ArmorSlotIndex].quantity = selectedCharacter.equippedArmor != null ? 1 : 0;

            slots[BootsSlotIndex].item = selectedCharacter.equippedBoots;
            slots[BootsSlotIndex].quantity = selectedCharacter.equippedBoots != null ? 1 : 0;

            slots[HelmSlotIndex].item = selectedCharacter.equippedHelm;
            slots[HelmSlotIndex].quantity = selectedCharacter.equippedHelm != null ? 1 : 0;
        }
        else
        {
            for (int i = 0; i < ReservedSlotCount; i++)
            {
                slots[i].item = null;
                slots[i].quantity = 0;
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item != null)
            {
                uiSlots[i].Set(slots[i]);
            }
            else
            {
                uiSlots[i].Clear();
            }
        }

        if (currentCharacterText != null && selectedCharacter != null)
            currentCharacterText.text = $"Character: {selectedCharacter.characterName}";
    }

    public void SelectItem(int index)
    {
        if (slots[index].item == null)
            return;

        selectedItem = slots[index];
        selectedItemIndex = index;

        selectedItemName.text = selectedItem.item.displayName;
        selectedItemDescription.text = selectedItem.item.description;

        selectedItemstatName.text = string.Empty;
        selectedItemsStatValue.text = string.Empty;

        foreach (var stat in selectedItem.item.statsOfItem)
        {
            selectedItemstatName.text += stat.type + "\n";
            selectedItemsStatValue.text += "+ " + stat.value + "\n";
        }

        bool isEquipSlot = index >= 0 && index < ReservedSlotCount;
        ItemType type = selectedItem.item.type;

        useButton.SetActive(type == ItemType.Consumable);
        equipButton.SetActive(!isEquipSlot && IsEquipableType(type));
        unequipButton.SetActive(isEquipSlot && selectedCharacter != null && selectedCharacter.GetEquippedItem(type) != null);
    }

    bool IsEquipableType(ItemType type)
    {
        return type == ItemType.EquipableWeapon ||
               type == ItemType.EquipableArmor ||
               type == ItemType.EquipableBoots ||
               type == ItemType.EquipableHelm;
    }

    void ClearSelectedItemWindow()
    {
        selectedItem = null;
        selectedItemName.text = string.Empty;
        selectedItemDescription.text = string.Empty;
        selectedItemstatName.text = string.Empty;
        selectedItemsStatValue.text = string.Empty;
        useButton.SetActive(false);
        equipButton.SetActive(false);
        unequipButton.SetActive(false);
    }

    public void RemoveSelectedItem()
    {
        selectedItem.quantity--;
        if (selectedItem.quantity == 0)
        {
            selectedItem.item = null;
            ClearSelectedItemWindow();
        }
        SaveManager.instance.SaveInventory();
        UpdateUI();
    }

    public void OnUseButton()
    {
        if (selectedItem == null || selectedItem.item == null || selectedCharacter == null)
            return;

        if (selectedItem.item.type == ItemType.Consumable)
        {
            foreach (var stat in selectedItem.item.statsOfItem)
            {
                switch (stat.type)
                {
                    case StatsType.HP:
                        selectedCharacter.maxHealth += Mathf.RoundToInt(stat.value);
                        break;
                    case StatsType.MP:
                        selectedCharacter.mana += Mathf.RoundToInt(stat.value);
                        break;
                    default:
                        Debug.Log($"Stat {stat.type} is not used for consumables.");
                        break;
                }
            }

            selectedCharacter.RecalculateStats();
        }

        RemoveSelectedItem();
        selectedCharacter.RecalculateStats();

        if (lastSelectedCharacterIndex >= 0)
            ShowCharacterStats(lastSelectedCharacterIndex);

        UpdateUI();
        SaveManager.instance.SaveInventory();
    }

    public void OnEquipButton()
    {
        if (selectedCharacter == null || selectedItem == null)
            return;

        ItemData itemToEquip = selectedItem.item;
        ItemType itemType = itemToEquip.type;

        int slotIndex = itemType switch
        {
            ItemType.EquipableWeapon => WeaponSlotIndex,
            ItemType.EquipableArmor => ArmorSlotIndex,
            ItemType.EquipableBoots => BootsSlotIndex,
            ItemType.EquipableHelm => HelmSlotIndex,
            _ => -1
        };

        if (slotIndex == -1) return;

        // 先卸下舊裝備
        ItemData oldItem = selectedCharacter.Unequip(itemType);
        if (oldItem != null)
            AddItem(oldItem);

        // 明確設定新裝備
        switch (itemType)
        {
            case ItemType.EquipableWeapon:
                selectedCharacter.equippedWeapon = itemToEquip;
                Debug.Log($"{selectedCharacter.characterName} 裝備武器: {itemToEquip.name} (成功設定 equippedWeapon)");
                break;
            case ItemType.EquipableArmor:
                selectedCharacter.equippedArmor = itemToEquip;
                Debug.Log($"{selectedCharacter.characterName} 裝備護甲: {itemToEquip.name}");
                break;
            case ItemType.EquipableBoots:
                selectedCharacter.equippedBoots = itemToEquip;
                Debug.Log($"{selectedCharacter.characterName} 裝備靴子: {itemToEquip.name}");
                break;
            case ItemType.EquipableHelm:
                selectedCharacter.equippedHelm = itemToEquip;
                Debug.Log($"{selectedCharacter.characterName} 裝備頭盔: {itemToEquip.name}");
                break;
        }

        selectedCharacter.RecalculateStats();

        // 更新裝備槽 UI
        slots[slotIndex].item = itemToEquip;
        slots[slotIndex].quantity = 1;

        // 移除背包物品
        slots[selectedItemIndex].quantity--;
        if (slots[selectedItemIndex].quantity <= 0)
            slots[selectedItemIndex].item = null;

        ClearSelectedItemWindow();
        if (lastSelectedCharacterIndex >= 0)
            ShowCharacterStats(lastSelectedCharacterIndex);

        UpdateUI();
        SaveManager.instance.SaveInventory();
    }

    public void OnUnequipButton()
    {
        if (selectedCharacter == null || selectedItem == null)
            return;

        ItemType type = selectedItem.item.type;
        int index = selectedItemIndex;

        if (index < 0 || index >= ReservedSlotCount)
            return;

        ItemData unequipped = selectedCharacter.Unequip(type);
        selectedCharacter.RecalculateStats();

        if (unequipped != null)
        {
            slots[index].item = null;
            slots[index].quantity = 0;
            AddItem(unequipped);
        }

        ClearSelectedItemWindow();

        if (lastSelectedCharacterIndex >= 0)
            ShowCharacterStats(lastSelectedCharacterIndex);
        SaveManager.instance.SaveInventory();
        UpdateUI();
    }

    public void RemoveItem(ItemData item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == item)
            {
                slots[i].quantity--;

                if (slots[i].quantity == 0)
                {
                    slots[i].item = null;
                    ClearSelectedItemWindow();
                }

                UpdateUI();
                return;
            }
        }
    }

    public int GetItemQuantity(ItemData item)
    {
        int total = 0;
        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)
        {
            if (slots[i].item == item)
            {
                total += slots[i].quantity;
            }
        }
        return total;
    }

    public void RemoveItemQuantity(ItemData item, int amount)
    {
        int remaining = amount;

        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)
        {
            if (slots[i].item == item && remaining > 0)
            {
                int deduct = Mathf.Min(remaining, slots[i].quantity);
                slots[i].quantity -= deduct;
                remaining -= deduct;

                if (slots[i].quantity <= 0)
                {
                    slots[i].item = null;
                    slots[i].quantity = 0;
                }
            }
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"材料不足！還缺 {remaining} 個 {item.displayName}");
        }

        UpdateUI();
        SaveManager.instance.SaveInventory();
    }

    [ContextMenu("清空背包 (測試用)")]
    public void ClearInventory()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].item = null;
            slots[i].quantity = 0;
        }
        UpdateUI();
        SaveManager.instance.SaveInventory();
        Debug.Log("背包已清空！");
    }

    public bool HasItem(ItemData item, int quantity)
    {
        int amount = 0;

        for (int i = 0; i < slots.Length; i++)
        {

            if (slots[i].item == item)
            {
                amount += slots[i].quantity;
            }

            if (amount >= quantity)
            {
                return true;
            }


        }

        return false;
    }

    public void SortInventory()
    {
        // Extract the general inventory part (after reserved slots)
        List<ItemSlot> generalInventory = new List<ItemSlot>();

        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)
        {
            if (slots[i].item != null)
            {
                generalInventory.Add(new ItemSlot
                {
                    item = slots[i].item,
                    quantity = slots[i].quantity
                });
            }
        }

        // Sort by item name (you can change this to item.type or any custom logic)
        generalInventory.Sort((a, b) => a.item.displayName.CompareTo(b.item.displayName));

        // Clear the general inventory part of the slots
        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)
        {
            slots[i].item = null;
            slots[i].quantity = 0;
        }

        // Reassign the sorted items back to the inventory slots
        for (int i = 0; i < generalInventory.Count; i++)
        {
            int slotIndex = GeneralInventoryStartIndex + i;
            slots[slotIndex].item = generalInventory[i].item;
            slots[slotIndex].quantity = generalInventory[i].quantity;
        }

        UpdateUI();
        SaveManager.instance.SaveInventory();
    }

    public void ToggleInventory()
    {
        SetupButtons();
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);

        if (inventoryPanel.activeSelf)
        {
            if (TeamManager.Instance.playerTeamCharacters.Count > 0)
                ShowCharacterStats(0); // Automatically select Player 1
        }

        ClearSelectedItemWindow();
        SortInventory();
    }

}
[System.Serializable]
public class ItemSlot
{
    public ItemData item;
    public int quantity;
}
