using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 戰鬥狀態枚舉：開始、玩家回合、敵人回合、勝利、失敗
/// </summary>
public enum BattleState { START, PLAYER_TURN, ENEMY_TURN, WON, LOST }

public class BattleController : MonoBehaviour
{
    public static BattleController instance;

    [Header("狀態監控")]
    public BattleState state;
    public int turnCount = 0;

    [Header("角色引用")]
    public Animator playerAnimator;
    public Character currentPlayer;

    [Header("視覺特效 (VFX)")]
    public GameObject vfxSwordSlash;
    public GameObject vfxMagicExplosion;
    public GameObject vfxEnemyHit;

    [Header("相機功能")]
    public CameraShake cameraShake;

    [Header("音頻資源")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip bgmBattle;
    public AudioClip sfxSword;
    public AudioClip sfxMagic;
    public AudioClip sfxCorrect;
    public AudioClip sfxWrong;
    public AudioClip sfxHurt;
    public AudioClip sfxEnemyHurt;
    public AudioClip sfxWin;
    public AudioClip sfxLose;
    public AudioClip sfxClick;

    [Header("UI 介面")]
    public TextMeshProUGUI txtTurnIndicator;
    public TextMeshProUGUI txtTurnCount;
    public TextMeshProUGUI txtCombatLog;
    public BattleUIManager uiManager;

    [Header("數值顯示")]
    public TextMeshProUGUI txtPlayerName;
    public TextMeshProUGUI txtPlayerHP;
    public Slider sliderPlayerHP;
    public TextMeshProUGUI txtEnemyName;
    public TextMeshProUGUI txtEnemyHP;
    public Slider sliderEnemyHP;

    [Header("戰鬥參數")]
    private int playerCurrentHP;
    public EnemyData currentEnemy;

    [Header("學習數據結算")]
    public int correctCount = 0;
    public int wrongCount = 0;

    [Header("結算 UI 面板")]
    public GameObject endReportPanel;
    public TextMeshProUGUI endReportText;

    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // 初始化戰鬥數據
        state = BattleState.START;
        turnCount = 0;
        correctCount = 0;
        wrongCount = 0;
        txtPlayerName.text = "準備中...";
        UpdateCombatLog("");

        // 背景音樂播放
        if (musicSource != null && bgmBattle != null)
        {
            musicSource.clip = bgmBattle;
            musicSource.Play();
        }

        // 自動獲取必要組件引用
        if (cameraShake == null) cameraShake = FindObjectOfType<CameraShake>();
        if (uiManager == null) uiManager = FindObjectOfType<BattleUIManager>();

        EnsureEndReportUI();
        if (endReportPanel != null) endReportPanel.SetActive(false);

        Invoke("SetupBattle", 0.1f);
    }

    /// <summary>
    /// 初始化戰鬥場景、加載隊伍角色及設置位置
    /// </summary>
    void SetupBattle()
    {
        // 隊伍為空時的備份載入邏輯
        if (TeamManager.Instance != null && TeamManager.Instance.playerTeamCharacters.Count == 0)
        {
            GameObject p0 = Resources.Load<GameObject>("Characters/Bella");
            GameObject p1 = Resources.Load<GameObject>("Characters/Kael");
            GameObject p2 = Resources.Load<GameObject>("Characters/Mia");

            if (p0 != null) TeamManager.Instance.AddToTeam(p0);
            if (p1 != null) TeamManager.Instance.AddToTeam(p1);
            if (p2 != null) TeamManager.Instance.AddToTeam(p2);
        }

        if (TeamManager.Instance == null || TeamManager.Instance.playerTeamCharacters.Count == 0)
        {
            Debug.LogError("無法載入角色資源");
            return;
        }

        // 依據角色名稱進行排序（Kael > Bella > Mia）
        TeamManager.Instance.playerTeamCharacters.Sort((a, b) =>
        {
            int GetOrder(Character c)
            {
                if (c == null || string.IsNullOrEmpty(c.characterName)) return 99;
                string name = c.characterName.ToLower();
                if (name.Contains("kael") || name.Contains("keal")) return 0;
                if (name.Contains("bella")) return 1;
                if (name.Contains("mia")) return 2;
                return 10;
            }
            return GetOrder(a).CompareTo(GetOrder(b));
        });

        // 隱藏非戰鬥場景角色
        GameObject samurai = GameObject.Find("Samurai");
        if (samurai != null) samurai.SetActive(false);

        // 載入敵人資料
        currentEnemy = FindObjectOfType<EnemyData>();
        if (currentEnemy != null)
        {
            txtEnemyName.text = currentEnemy.monsterName;
            sliderEnemyHP.maxValue = currentEnemy.maxHP;
        }

        if (QuestionManager.instance != null)
            QuestionManager.instance.ResetMathQuestionUsageForBattle();

        // 預設戰鬥位置坐標
        Vector3[] positions = new Vector3[] {
            new Vector3(-6f, 55f, -1f),
            new Vector3(-8f, 55f, -1f),
            new Vector3(-10f, 55f, -1f)
        };

        // 初始化所有隊員狀態與位置
        for (int i = 0; i < TeamManager.Instance.playerTeamCharacters.Count; i++)
        {
            Character c = TeamManager.Instance.playerTeamCharacters[i];
            if (c != null)
            {
                c.gameObject.SetActive(true);
                c.health = c.maxHealth;

                if (i < positions.Length)
                {
                    c.transform.position = positions[i];
                }
            }
        }

        SwitchCharacter(0);
        UpdateUI();
        StartCoroutine(BattleStartFlow());
    }

    IEnumerator BattleStartFlow()
    {
        uiManager.SetInteractable(false);
        ShowTurnText("遭遇敵人！");
        UpdateTurnCountUI();
        yield return new WaitForSeconds(2f);
        state = BattleState.PLAYER_TURN;
        PlayerTurn();
    }

    void PlayerTurn()
    {
        turnCount++;
        UpdateTurnCountUI();
        ShowTurnText("你的回合");
        UpdateCombatLog("");
        uiManager.SetInteractable(true);
    }

    /// <summary>
    /// 玩家普通物理攻擊邏輯
    /// </summary>
    public void PlayerAttack_Normal()
    {
        if (state != BattleState.PLAYER_TURN) return;
        if (currentPlayer == null || currentEnemy == null) return;

        float damageMultiplier = 0.05f;
        int totalDamage = Mathf.RoundToInt(currentPlayer.characterData.baseAttackPower * damageMultiplier);

        string msg = $"物理攻擊太弱了！<color=red>對知識怪獸無效！</color>造成 {totalDamage} 點傷害。";
        StartCoroutine(PlayerAttackSequence("Attack", totalDamage, msg, vfxSwordSlash, sfxSword, 0.1f));
    }

    /// <summary>
    /// 玩家魔法攻擊（學習答題結果相關）
    /// </summary>
    public void PlayerAttack_Magic(bool isCorrect, int attackAttributeID)
    {
        if (state != BattleState.PLAYER_TURN) return;
        if (currentPlayer == null || currentEnemy == null) return;

        if (isCorrect) correctCount++;
        else wrongCount++;

        // 計算魔法傷害基礎值（受 MP 與裝備加成）
        float baseMP = currentPlayer.characterData.baseMaxMana;
        float totalMP = currentPlayer.GetTotalStat(StatsType.MP);
        float baseAD = currentPlayer.characterData.baseAttackPower;
        float totalAD = currentPlayer.GetTotalStat(StatsType.AD);
        int baseSpellDamage = 150;
        int mpBonus = Mathf.RoundToInt(totalMP - baseMP);
        int adBonusFromGear = Mathf.Max(0, Mathf.RoundToInt(totalAD - baseAD));
        int totalDamage = baseSpellDamage + mpBonus + adBonusFromGear;

        string resultPrefix = "";
        string damageColor = "cyan";
        float shakePower = 0.1f;

        // 處理答題正確/錯誤的傷害修正與屬性加成
        if (isCorrect)
        {
            sfxSource.PlayOneShot(sfxCorrect);
            if (currentEnemy.attribute == attackAttributeID)
            {
                totalDamage = Mathf.RoundToInt(totalDamage * 1.5f);
                resultPrefix = "<color=red>答對了！</color>";
                damageColor = "yellow";
                shakePower = 0.4f;
            }
            else
            {
                resultPrefix = "<color=green>答對了！</color>";
                damageColor = "white";
                shakePower = 0.2f;
            }
        }
        else
        {
            sfxSource.PlayOneShot(sfxWrong);
            if (currentEnemy.attribute == attackAttributeID)
            {
                totalDamage = Mathf.RoundToInt(totalDamage * 0.3f);
                resultPrefix = "<color=grey>答錯了...</color>";
                damageColor = "grey";
            }
            else
            {
                totalDamage = Mathf.RoundToInt(totalDamage * 0.1f);
                resultPrefix = "<color=grey>答錯了...</color>";
                damageColor = "grey";
            }
        }

        // 處理學科專精加成
        float specializationMultiplier = GetSubjectSpecializationMultiplier(attackAttributeID);
        if (specializationMultiplier > 1f)
            totalDamage = Mathf.RoundToInt(totalDamage * specializationMultiplier);

        // 處理暴擊計算
        bool isCrit = Random.value <= currentPlayer.GetTotalStat(StatsType.CR);
        if (isCrit)
        {
            totalDamage = Mathf.RoundToInt(totalDamage * currentPlayer.GetTotalStat(StatsType.CRD));
            damageColor = "orange";
        }

        string msg = $"{resultPrefix} 對 {currentEnemy.monsterName} 造成了 <color={damageColor}><b>{totalDamage}</b></color> 點傷害。";
        if (isCrit) msg += " <color=red>暴擊！</color>";

        StartCoroutine(PlayerAttackSequence("Attack", totalDamage, msg, vfxMagicExplosion, sfxMagic, shakePower));

        if (cameraShake != null) cameraShake.Shake(shakePower + 0.1f, 0.15f);
    }

    public void PlayerRun()
    {
        if (state != BattleState.PLAYER_TURN) return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        uiManager.SetInteractable(false);
        txtTurnIndicator.gameObject.SetActive(false);
        UpdateCombatLog("<color=green>逃跑成功！</color>");
        if (sfxSource != null && sfxCorrect != null) sfxSource.PlayOneShot(sfxCorrect);
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("Map_MainCity");
    }

    /// <summary>
    /// 執行玩家攻擊序列，包含動畫、音效、特效與傷害結算
    /// </summary>
    IEnumerator PlayerAttackSequence(string animName, int damage, string customMessage, GameObject vfxPrefab, AudioClip soundClip, float shakePower)
    {
        uiManager.SetInteractable(false);
        txtTurnIndicator.gameObject.SetActive(false);

        if (playerAnimator != null) playerAnimator.SetTrigger(animName);

        yield return new WaitForSeconds(0.5f);

        bool isDead = false;

        if (currentEnemy != null)
        {
            PlayHitVFX(vfxPrefab, currentEnemy.transform.position);

            if (sfxSource != null && soundClip != null) sfxSource.PlayOneShot(soundClip);
            if (sfxSource != null && sfxEnemyHurt != null) sfxSource.PlayOneShot(sfxEnemyHurt);

            if (cameraShake != null && shakePower > 0)
            {
                StartCoroutine(cameraShake.Shake(0.15f, shakePower));
            }

            currentEnemy.TakeDamage(damage);
            UpdateUI();
            UpdateCombatLog(customMessage);

            if (currentEnemy.currentHP <= 0)
            {
                state = BattleState.WON;
                isDead = true;
                currentEnemy.PlayDeathAnim();
                EndBattle();
            }
        }

        if (!isDead)
        {
            yield return new WaitForSeconds(1.5f);
            state = BattleState.ENEMY_TURN;
            StartCoroutine(EnemyTurn());
        }
    }

    /// <summary>
    /// 執行敵方攻擊序列與玩家受傷邏輯
    /// </summary>
    IEnumerator EnemyTurn()
    {
        ShowTurnText("敵方回合");
        yield return new WaitForSeconds(1f);
        txtTurnIndicator.gameObject.SetActive(false);
        if (currentEnemy != null) currentEnemy.PlayAttackAnim();
        yield return new WaitForSeconds(0.5f);

        if (playerAnimator != null)
        {
            PlayHitVFX(vfxEnemyHit, playerAnimator.transform.position);
            if (sfxSource != null && sfxHurt != null) sfxSource.PlayOneShot(sfxHurt);
            if (cameraShake != null) StartCoroutine(cameraShake.Shake(0.2f, 0.3f));

            foreach (AnimatorControllerParameter param in playerAnimator.parameters)
            {
                if (param.name == "Hurt") playerAnimator.SetTrigger("Hurt");
                if (param.name == "Hit") playerAnimator.SetTrigger("Hit");
            }
        }

        // 計算敵方傷害（考慮防禦力減傷）
        int enemyDmg = (currentEnemy != null) ? currentEnemy.damage : 50;
        float reductionMultiplier = 100f / (100f + currentPlayer.GetTotalStat(StatsType.DF));
        int finalDmg = Mathf.RoundToInt(enemyDmg * reductionMultiplier);
        finalDmg = Mathf.Max(Mathf.RoundToInt(enemyDmg * 0.1f), finalDmg);

        playerCurrentHP -= finalDmg;
        if (playerCurrentHP < 0) playerCurrentHP = 0;

        UpdateUI();
        UpdateCombatLog($"敵方發動攻擊！你受到了 <color=red>{finalDmg}</color> 點傷害！");

        if (playerCurrentHP <= 0)
        {
            state = BattleState.LOST;
            EndBattle();
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
            state = BattleState.PLAYER_TURN;
            PlayerTurn();
        }
    }

    void PlayHitVFX(GameObject vfx, Vector3 targetPos)
    {
        if (vfx == null) return;
        Vector3 spawnPos = new Vector3(targetPos.x, targetPos.y + 0.5f, -1f);
        GameObject effect = Instantiate(vfx, spawnPos, Quaternion.identity);
        Destroy(effect, 2f);
    }

    /// <summary>
    /// 戰鬥結束處理：發放獎勵、更新狀態與播放結算音效
    /// </summary>
    void EndBattle()
    {
        if (musicSource != null) musicSource.Stop();

        // 隱藏戰鬥過程中的動態 UI
        if (txtCombatLog != null) txtCombatLog.gameObject.SetActive(false);
        if (txtTurnIndicator != null) txtTurnIndicator.gameObject.SetActive(false);

        if (state == BattleState.WON)
        {
            // 依據關卡等級獲取金幣
            int earnedGold = GameData.chosenLevel;
            GoldManager.instance.AddGold(earnedGold);

            if (sfxSource != null && sfxWin != null) sfxSource.PlayOneShot(sfxWin);
        }
        else if (state == BattleState.LOST)
        {
            if (playerAnimator != null)
            {
                foreach (AnimatorControllerParameter param in playerAnimator.parameters)
                {
                    if (param.name == "Die") playerAnimator.SetTrigger("Die");
                    if (param.name == "Death") playerAnimator.SetTrigger("Death");
                }
            }
            if (sfxSource != null && sfxLose != null) sfxSource.PlayOneShot(sfxLose);
        }

        ShowEndReport();
        StartCoroutine(ReturnToMenu());
    }

    IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(5f);
        if (endReportPanel != null) endReportPanel.SetActive(false);
        SceneManager.LoadScene("Map_MainCity");
    }

    void UpdateUI()
    {
        if (currentPlayer != null)
        {
            sliderPlayerHP.maxValue = currentPlayer.maxHealth;
            sliderPlayerHP.value = playerCurrentHP;
            txtPlayerHP.text = $"{playerCurrentHP} / {currentPlayer.maxHealth}";
        }
        if (currentEnemy != null)
        {
            sliderEnemyHP.value = currentEnemy.currentHP;
            txtEnemyHP.text = $"{currentEnemy.currentHP} / {currentEnemy.maxHP}";
        }
    }

    void UpdateTurnCountUI() { if (txtTurnCount != null) txtTurnCount.text = $"{turnCount}T"; }
    void ShowTurnText(string text) { if (txtTurnIndicator != null) { txtTurnIndicator.text = text; txtTurnIndicator.gameObject.SetActive(true); } }
    void UpdateCombatLog(string text) { if (txtCombatLog != null) { txtCombatLog.text = text; txtCombatLog.gameObject.SetActive(true); } }

    /// <summary>
    /// 獲取角色對應學科的屬性傷害乘數
    /// </summary>
    float GetSubjectSpecializationMultiplier(int attackAttributeID)
    {
        if (currentPlayer == null || string.IsNullOrEmpty(currentPlayer.characterName)) return 1f;
        string n = currentPlayer.characterName.ToLowerInvariant();
        if (n.Contains("mia") && attackAttributeID == 0) return 1.2f;
        if (n.Contains("bella") && attackAttributeID == 1) return 1.2f;
        if ((n.Contains("kael") || n.Contains("keal")) && attackAttributeID == 2) return 1.2f;
        return 1f;
    }

    /// <summary>
    /// 切換玩家角色，並保存當前角色的剩餘血量
    /// </summary>
    public void SwitchCharacter(int teamIndex)
    {
        if (TeamManager.Instance == null || teamIndex >= TeamManager.Instance.playerTeamCharacters.Count)
        {
            Debug.LogWarning($"無效的隊伍索引: {teamIndex}");
            return;
        }

        // 保存換人前的血量狀態
        if (currentPlayer != null)
        {
            currentPlayer.health = playerCurrentHP;
        }

        // 設置活動角色物件與位置
        for (int i = 0; i < TeamManager.Instance.playerTeamCharacters.Count; i++)
        {
            Character c = TeamManager.Instance.playerTeamCharacters[i];
            if (c != null)
            {
                bool isSelected = (i == teamIndex);
                c.gameObject.SetActive(isSelected);
                if (isSelected) c.transform.position = new Vector3(-6f, 55f, -1f);
            }
        }

        currentPlayer = TeamManager.Instance.playerTeamCharacters[teamIndex];
        playerAnimator = currentPlayer.GetComponentInChildren<Animator>();

        // 重新計算角色裝備加成後的數值
        currentPlayer.RecalculateStats();

        // 載入該角色之前剩餘的血量
        playerCurrentHP = currentPlayer.health;

        txtPlayerName.text = currentPlayer.characterName;
        UpdateUI();
        UpdateCombatLog($"切換至 <color=yellow>{currentPlayer.characterName}</color>！");
    }

    /// <summary>
    /// 確保結算介面（Panel 與 Text）已在 Canvas 下正確生成並配置
    /// </summary>
    void EnsureEndReportUI()
    {
        if (endReportPanel != null && endReportText != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        if (endReportPanel == null)
        {
            // 創建背景遮罩
            endReportPanel = new GameObject("EndReportPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            endReportPanel.transform.SetParent(canvas.transform, false);

            var rt = endReportPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = endReportPanel.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
        }

        if (endReportText == null)
        {
            // 創建結算資訊卡片背景
            GameObject cardObj = new GameObject("EndReportCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.transform.SetParent(endReportPanel.transform, false);

            var crt = cardObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 100f);
            crt.sizeDelta = new Vector2(1000f, 500f);

            var cimg = cardObj.GetComponent<Image>();
            cimg.color = new Color(0f, 0f, 0f, 0.80f);

            // 創建文本顯示物件
            GameObject textObj = new GameObject("EndReportText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(cardObj.transform, false);

            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(40f, 30f);
            trt.offsetMax = new Vector2(-40f, -30f);

            endReportText = textObj.GetComponent<TextMeshProUGUI>();
            endReportText.alignment = TextAlignmentOptions.Center;
            endReportText.enableAutoSizing = true;
            endReportText.fontStyle = FontStyles.Bold;
            endReportText.fontSizeMax = 80;
            endReportText.fontSizeMin = 40;
            endReportText.fontSize = 70;
            endReportText.color = Color.white;
            endReportText.enableWordWrapping = true;
            endReportText.richText = true;

            // 設置字體以支援中文顯示
            if (txtCombatLog != null && txtCombatLog.font != null)
            {
                endReportText.font = txtCombatLog.font;
                endReportText.fontSharedMaterial = txtCombatLog.fontSharedMaterial;
            }
            else if (txtTurnIndicator != null && txtTurnIndicator.font != null)
            {
                endReportText.font = txtTurnIndicator.font;
                endReportText.fontSharedMaterial = txtTurnIndicator.fontSharedMaterial;
            }
        }

        endReportPanel.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 計算並顯示戰鬥結算報告
    /// </summary>
    void ShowEndReport()
    {
        EnsureEndReportUI();
        if (endReportPanel == null || endReportText == null) return;

        int total = correctCount + wrongCount;
        float acc = total > 0 ? (correctCount * 100f / total) : 0f;

        string titleText = "";
        if (state == BattleState.WON)
        {
            int earnedGold = GameData.chosenLevel;
            titleText = $"<color=#FFD700>勝利！獲得了 {earnedGold} 枚金幣。</color>\n\n";
        }
        else
        {
            titleText = "<color=#FF9999>戰鬥失敗...</color>\n\n";
        }

        endReportText.text = "<b>" +
            titleText +
            $"學習結算報告\n" +
            $"答對：<color=#55FF55>{correctCount}</color>，答錯：<color=#FF5555>{wrongCount}</color>\n" +
            $"正確率：{acc:0.#}%" +
            "</b>";

        endReportPanel.SetActive(true);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 離開戰鬥場景時銷毀結算面板，防止殘留在持久化 Canvas
        if (scene.name != "BattleScene")
        {
            if (endReportPanel != null)
            {
                Destroy(endReportPanel);
                endReportPanel = null;
                endReportText = null;
            }
        }
    }
}