
using UnityEngine;

public class ObjectActivator : MonoBehaviour
{
    public GameObject objectToActive;
    public GameObject talkIcon;
    private bool playerInRange;


    private void Update()
    {
        if (playerInRange)
        {
            if (Input.GetKeyDown(KeyCode.E) && !objectToActive.activeSelf)
            {
                objectToActive.SetActive(true);
                Time.timeScale = 0; // 暫停時間
            }
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (talkIcon != null)
            {
                talkIcon.SetActive(true);
            }


        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (talkIcon != null)
            {
                talkIcon.SetActive(false);
            }

        }
    }
}
