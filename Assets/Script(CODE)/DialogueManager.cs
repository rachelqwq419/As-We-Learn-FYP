using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    public string name;            
    public Sprite portrait;        
    public AudioClip voiceClip;    
    [TextArea(3, 10)]
    public string sentence;        
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [Header("UI 引用")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;

    [Header("音效組件")]
    public AudioSource voiceSource;      

    [Header("對話內容")]
    public DialogueLine[] dialogueLines;

    private int index;

    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        index = 0;
        if (dialogueLines.Length > 0)
        {
            DisplayNextSentence();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            DisplayNextSentence();
        }
    }

    public void DisplayNextSentence()
    {
        if (index < dialogueLines.Length)
        {
            // 1. 更新文字同頭像
            nameText.text = dialogueLines[index].name;
            dialogueText.text = dialogueLines[index].sentence;
            portraitImage.sprite = dialogueLines[index].portrait;

            // 2. 播放配音
            if (voiceSource != null && dialogueLines[index].voiceClip != null)
            {
                voiceSource.Stop(); 
                voiceSource.clip = dialogueLines[index].voiceClip;
                voiceSource.Play();
            }

            index++;
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        if (voiceSource != null) voiceSource.Stop(); 
        dialoguePanel.SetActive(false);
        Time.timeScale = 1; 
    }
}