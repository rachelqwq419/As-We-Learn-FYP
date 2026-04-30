using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingWindow : MonoBehaviour
{
    public GameObject craftingPanel;
    public CraftingRecipeUI[] recipeUIs;
    public static CraftingWindow instance;

    private void Awake()
    {
        instance = this;
    }

    public void Craft(CraftingRecipe recipe)
    {
        // 檢查是否足夠材料
        for (int i = 0; i < recipe.costs.Length; i++)
        {
            int owned = InventoryManager.instance.GetItemQuantity(recipe.costs[i].item);
            if (owned < recipe.costs[i].quantity)
            {
                Debug.Log("材料不足，無法合成！");
                return;
            }
        }

        // 扣除所有材料
        for (int i = 0; i < recipe.costs.Length; i++)
        {
            InventoryManager.instance.RemoveItemQuantity(recipe.costs[i].item, recipe.costs[i].quantity);
        }

        // 添加合成物品
        InventoryManager.instance.AddItem(recipe.itemToCraft);

        // 更新所有合成按鈕的狀態
        for (int i = 0; i < recipeUIs.Length; i++)
        {
            recipeUIs[i].UpdateCanCraft();
        }

        Debug.Log("合成成功：" + recipe.itemToCraft.displayName);
    }

    public void OncloseCraftingPanel()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    public void OnOpenCraftingPanel()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0;
    }
}
