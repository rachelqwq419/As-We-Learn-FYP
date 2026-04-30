using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Header("要前往的場景名稱")]
    public string targetSceneName;

    [Header("科目區域名稱 (打 Chinese/English/Math)")]
    public string subjectName;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(subjectName))
            {
                PlayerPrefs.SetString("CurrentSubject", subjectName);
            }

            SceneManager.LoadScene(targetSceneName);
        }
    }
}