using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubjectFilter : MonoBehaviour
{
    public GameObject btnChinese;
    public GameObject btnEnglish;
    public GameObject btnMath;

    void Start()
    {
        string subject = PlayerPrefs.GetString("CurrentSubject", "None");

        btnChinese.SetActive(false);
        btnEnglish.SetActive(false);
        btnMath.SetActive(false);

        if (subject == "Chinese")
        {
            btnChinese.SetActive(true);
            CenterButton(btnChinese);
        }
        else if (subject == "English")
        {
            btnEnglish.SetActive(true);
            CenterButton(btnEnglish);
        }
        else if (subject == "Math")
        {
            btnMath.SetActive(true);
            CenterButton(btnMath);
        }
        else
        {
            btnChinese.SetActive(true);
            btnEnglish.SetActive(true);
            btnMath.SetActive(true);
        }
    }

    void CenterButton(GameObject btn)
    {
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, rt.anchoredPosition.y);
    }
}