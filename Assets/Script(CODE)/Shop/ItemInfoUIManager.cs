
using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class ItemInfoUIManager : MonoBehaviour
{

    public static ItemInfoUIManager instance;
    public TextMeshProUGUI infoText;
    public ItemData selectedItem;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPrice;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI itemDescription;
    public GameObject buyButton;
    public GameObject shopPanel;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    void Start()
    {
        UpdateGold();
        itemNameText.text = string.Empty;
        itemDescription.text = string.Empty;
        itemPrice.text = string.Empty;
        infoText.text = string.Empty;
        buyButton.SetActive(false);
    }

    public void UpdateGold()
    {
        goldText.text = GoldManager.instance.gold.ToString();
    }

    public void DisplayItemInfo(ItemData item)
    {
        if (item == null) return;
        buyButton.SetActive(true);
        selectedItem = item;
        itemNameText.text = item.displayName;
        itemDescription.text = item.description;
        itemPrice.text = item.buyPrice.ToString();
        StringBuilder sb = new StringBuilder();

        if (item.statsOfItem != null && item.statsOfItem.Length > 0)
        {
            sb.AppendLine("<b>Stats:</b>");
            foreach (var stat in item.statsOfItem)
            {
                sb.AppendLine($"{stat.type} <color=green>+{stat.value}</color>");
            }
        }

        infoText.text = sb.ToString();
    }

    public void BuySelectedItem()
    {
        if (selectedItem == null) return;

        if (GoldManager.instance.gold >= selectedItem.buyPrice)
        {
            GoldManager.instance.ReduceGold(selectedItem.buyPrice);
            InventoryManager.instance.AddItem(selectedItem);
            Debug.Log("Item bought: " + selectedItem.displayName);
            UpdateGold();
        }
        else
        {
            Debug.Log("Not enough gold!");
        }
    }

    public void OncloseShopPanel()
    {
        shopPanel.SetActive(false);
        Time.timeScale = 1;
    }

    public void OnOpenShopPanel()
    {
        shopPanel.SetActive(true);
        Time.timeScale = 0;
    }
    // Start is called before the first frame update


    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDisable()
    {
        Time.timeScale = 1;
    }
}
