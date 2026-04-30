using UnityEngine;
using System.Collections;

public class EnemyData : MonoBehaviour
{
    [Header("基本資料")]
    public string monsterName;

    // 0 = 中文 (Chinese)
    // 1 = 英文 (English)
    // 2 = 數學 (Math)
    [Header("屬性 (0=中, 1=英, 2=數)")]
    public int attribute;

    [Header("戰鬥數值")]
    public int maxHP;
    public int damage;

    [Header("動態狀態")]
    public int currentHP;

    private SpriteRenderer sp;
    private Animator anim;

    void Start()
    {
        currentHP = maxHP;
        sp = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    public void TakeDamage(int dmg)
    {
        currentHP -= dmg;
        if (currentHP < 0) currentHP = 0;
        StopAllCoroutines();
        StartCoroutine(HitEffect());
    }

    IEnumerator HitEffect()
    {
        if (sp != null) sp.color = new Color(1, 0.5f, 0.5f);
        Vector3 originalPos = transform.position;
        transform.position = originalPos + new Vector3(0.1f, -0.1f, 0);
        yield return new WaitForSeconds(0.05f);
        transform.position = originalPos + new Vector3(-0.1f, 0.1f, 0);
        yield return new WaitForSeconds(0.05f);
        transform.position = originalPos;
        if (sp != null) sp.color = Color.white;
    }

    public void PlayAttackAnim()
    {
        if (anim != null) anim.SetTrigger("Attack");
    }

    public void PlayDeathAnim()
    {
        if (anim != null) anim.SetTrigger("Die");
    }
}