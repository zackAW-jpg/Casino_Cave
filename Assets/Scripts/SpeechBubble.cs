using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class SpeechBubble : MonoBehaviour
{
    [Header("References")]
    public GameObject messageObject;
    public Image bubbleImage;

    [Header("Options")]
    public float autoHideAfterSeconds = 4f;
    public bool hideWhenPlayerLeaves = true;

    private Canvas _canvas;
    private float _hideTime = -1f;
    private Graphic _messageGraphic;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        if (_canvas != null)
            _canvas.sortingOrder = 100;
        if (messageObject == null)
            messageObject = GetComponentInChildren<Text>()?.gameObject;
        if (messageObject != null)
            _messageGraphic = messageObject.GetComponent<Graphic>();
        if (bubbleImage == null)
            bubbleImage = GetComponentInChildren<Image>();
        Hide();
    }

    void Update()
    {
        if (_hideTime > 0f && Time.time >= _hideTime)
        {
            _hideTime = -1f;
            Hide();
        }
    }

    static void SetMessageText(GameObject go, string text)
    {
        if (go == null) return;
        var uitext = go.GetComponent<Text>();
        if (uitext != null)
        {
            uitext.text = text;
            return;
        }

        var tmp = go.GetComponent("TMPro.TMP_Text");
        if (tmp == null) return;

        var type = tmp.GetType();
        type.GetProperty("text")?.SetValue(tmp, text);
        
        type.GetMethod("SetAllDirty", BindingFlags.Instance | BindingFlags.Public)?.Invoke(tmp, null);
        type.GetMethod("ForceMeshUpdate", Type.EmptyTypes)?.Invoke(tmp, null);
    }

    public void Show(string text)
    {
        SetMessageText(messageObject, text);
        if (_canvas != null)
            _canvas.enabled = true;
        if (bubbleImage != null)
            bubbleImage.enabled = true;
        if (_messageGraphic != null)
            _messageGraphic.enabled = true;
        if (autoHideAfterSeconds > 0f)
            _hideTime = Time.time + autoHideAfterSeconds;
    }

    public void Hide()
    {
        _hideTime = -1f;
        if (_canvas != null)
            _canvas.enabled = false;
        if (bubbleImage != null)
            bubbleImage.enabled = false;
        if (_messageGraphic != null)
            _messageGraphic.enabled = false;
    }
}
