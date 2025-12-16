using UnityEngine;
using TMPro;

public class ChatBubbleSizeHandler : MonoBehaviour
{
    [SerializeField] RectTransform imageRec;
    [SerializeField] RectTransform textRec;
    [SerializeField] TMP_Text text;
    
    public void Init(string inputText)
    {
        text.text = inputText;
        imageRec.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textRec.rect.height);
    }
}
