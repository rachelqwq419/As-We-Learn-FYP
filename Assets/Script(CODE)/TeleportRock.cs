using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 傳送石碑：處理玩家碰撞偵測、畫面淡出與場景切換邏輯。
/// </summary>
public class TeleportRock : MonoBehaviour
{
    [Header("目標場景配置")]
    public string targetScene = "MenuScene";

    [Header("UI 組件")]
    public Image fadePanel; // 畫面漸變用的遮罩面板

    private bool isTriggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 透過 Rigidbody2D 確認碰撞對象為玩家，並防止重複觸發
        if (other.GetComponent<Rigidbody2D>() != null && !isTriggered)
        {
            isTriggered = true;
            StartCoroutine(TransitionAndLoad());
        }
    }

    /// <summary>
    /// 執行淡出動畫並切換至目標場景
    /// </summary>
    IEnumerator TransitionAndLoad()
    {
        // 1. 處理遮罩面板的 Alpha 漸變 (Fade In)
        if (fadePanel != null)
        {
            float timer = 0f;
            float duration = 1.0f; // 動態效果總時長

            while (timer < duration)
            {
                timer += Time.deltaTime;

                // 根據時間比例計算透明度
                Color c = fadePanel.color;
                c.a = timer / duration;
                fadePanel.color = c;
                yield return null;
            }

            // 強制設定為完全不透明
            Color finalColor = fadePanel.color;
            finalColor.a = 1f;
            fadePanel.color = finalColor;
        }

        // 2. 在全黑狀態下稍作停留，增加過場穩定度
        yield return new WaitForSeconds(0.5f);

        // 3. 執行場景加載
        SceneManager.LoadScene(targetScene);
    }
}