using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuController : MonoBehaviour
{
    [Header("UI 設定")]
    public Image fadePanel;
    public Button startButton;

    [Header("目標場景")]
    public string gameSceneName = "Example";

    // 🔥 NEW: 音效相關變數
    [Header("音效設定")]
    public AudioSource audioSource; // 用來播聲音的喇叭
    public AudioClip startSound;    // 開始遊戲的音效 (例如 UI_Click)

    void Start()
    {
        // 綁定按鈕功能
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartGameClicked);
        }

        StartCoroutine(FadeIn());
    }

    // === 按鈕點擊事件 ===
    void OnStartGameClicked()
    {
        //播放音效
        if (audioSource != null && startSound != null)
        {
            audioSource.PlayOneShot(startSound);
        }

        // 執行轉場
        StartCoroutine(FadeOutAndLoad());
    }

    // === 淡入 (Fade In) ===
    IEnumerator FadeIn()
    {
        if (fadePanel != null)
        {
            float timer = 0f;
            float duration = 1.0f;
            fadePanel.color = Color.black;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                Color c = fadePanel.color;
                c.a = 1f - (timer / duration);
                fadePanel.color = c;
                yield return null;
            }
            Color finalColor = fadePanel.color;
            finalColor.a = 0f;
            fadePanel.color = finalColor;
            fadePanel.raycastTarget = false;
        }
    }

    // === 淡出 (Fade Out) ===
    IEnumerator FadeOutAndLoad()
    {
        if (fadePanel != null)
        {
            fadePanel.raycastTarget = true;
            float timer = 0f;
            float duration = 1.0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                Color c = fadePanel.color;
                c.a = timer / duration;
                fadePanel.color = c;
                yield return null;
            }
            fadePanel.color = Color.black;
        }

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(gameSceneName);
    }
}