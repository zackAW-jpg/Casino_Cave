using TMPro;
using UnityEngine;

[RequireComponent(typeof(PlayerHealChannel))]
public class PlayerHealChannelLabel : MonoBehaviour
{
    static readonly Color LabelColor = new Color(0.78f, 0.78f, 0.78f);

    [SerializeField] TMP_FontAsset font;
    [SerializeField] Vector3 localOffset = new Vector3(0f, 0.55f, 0f);
    [SerializeField] float fontSize = 2.2f;

    PlayerHealChannel _healChannel;
    TextMeshPro _tmp;
    SpriteRenderer _sprite;

    void Awake()
    {
        _healChannel = GetComponent<PlayerHealChannel>();
        _sprite = GetComponent<SpriteRenderer>();

        var go = new GameObject("HealChannelLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localOffset;
        go.layer = gameObject.layer;

        _tmp = go.AddComponent<TextMeshPro>();
        _tmp.text = "healing...";
        _tmp.fontSize = fontSize;
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.color = LabelColor;
        if (font != null)
            _tmp.font = font;

        var mr = _tmp.GetComponent<MeshRenderer>();
        if (mr != null && _sprite != null)
        {
            mr.sortingLayerID = _sprite.sortingLayerID;
            mr.sortingOrder = _sprite.sortingOrder + 1;
        }

        _tmp.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (_tmp == null || _healChannel == null)
            return;

        bool show = _healChannel.IsChannelingHeal;
        if (_tmp.gameObject.activeSelf != show)
            _tmp.gameObject.SetActive(show);
    }
}
