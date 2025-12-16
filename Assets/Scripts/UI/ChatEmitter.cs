using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChatEmitter : MonoBehaviour
{
    [SerializeField] TMP_Text uiTextArea;
    [SerializeField] float displayDuration;
    Transform target;
    Vector3 offset;
    Coroutine displayChatMessage;

    void Awake()
    {
        if (Logger.DEBUG)
        {
            displayChatMessage = StartCoroutine(_DisplayChatMessage("[DEBUG: 생성된 텍스트]", displayDuration));   
        }
    }

    public void Bind(Transform followTarget, Vector3 offset)
    {
        target = followTarget;
        this.offset = offset;
    }

    void LateUpdate()
    {
        if (!target) return;
        if (displayChatMessage == null) return;

        transform.position = Camera.main.WorldToScreenPoint(target.position + offset);
    }

    public void SetChat(string text)
    {
        if (displayChatMessage != null)
            StopCoroutine(displayChatMessage);

        displayChatMessage = StartCoroutine(_DisplayChatMessage(text, displayDuration));   
    }

    IEnumerator _DisplayChatMessage(string text, float duration)
    {
        Logger.Write($"run coroutine; display chat message / text={text}, duration={duration}");
        uiTextArea.text = text;
        uiTextArea.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(duration);
        uiTextArea.text = "";
        uiTextArea.gameObject.SetActive(false);
    }

}
