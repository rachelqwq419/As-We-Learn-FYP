using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager instance;
    public int gold;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadGold();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        SaveGold();
    }

    public void ReduceGold(int amount)
    {
        gold -= amount;
        if (gold <= 0)
        {
            gold = 0;
        }

        SaveGold();
    }

    private void OnApplicationQuit()
    {
        SaveGold();
    }

    public void SaveGold()
    {
        string currentUser = PlayerPrefs.GetString("CurrentUser", "UnknownUser");

        string saveKey = "Gold_" + currentUser;

        PlayerPrefs.SetInt(saveKey, gold);
        PlayerPrefs.Save(); // Ensure it's written to disk
    }

    public void LoadGold()
    {
        string currentUser = PlayerPrefs.GetString("CurrentUser", "UnknownUser");

        string saveKey = "Gold_" + currentUser;

        gold = PlayerPrefs.GetInt(saveKey, 30);
    }
}