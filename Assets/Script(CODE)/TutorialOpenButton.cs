using UnityEngine;
[DisallowMultipleComponent]
public class TutorialOpenButton : MonoBehaviour
{
    public void OnClickOpenTutorial()
    {
        TutorialPanel.OpenTutorialStatic();
    }
}
