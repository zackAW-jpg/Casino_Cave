using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Canvas))]
public class DamagePopup : MonoBehaviour
{
    public float floatSpeed = 0.8f;
    public float lifetime = 0.6f;

    private Text _uiText;
    private TMP_Text _tmpText;
    private float _timer;
    private Color _startColor;

    private void Awake()
    {
        
        _uiText = GetComponentInChildren<Text>();
        if (_uiText != null)
        {
            _startColor = _uiText.color;
            return;
        }

        
        _tmpText = GetComponentInChildren<TMP_Text>();
        if (_tmpText != null)
        {
            _startColor = _tmpText.color;
        }
    }

    public void SetAmount(int amount)
    {
        string s = amount.ToString();

        if (_uiText != null)
            _uiText.text = s;
        if (_tmpText != null)
            _tmpText.text = s;
    }

    private void Update()
    {
        
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

        
        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / lifetime);
        float a = 1f - t;

        if (_uiText != null)
            _uiText.color = new Color(_startColor.r, _startColor.g, _startColor.b, a);

        if (_tmpText != null)
            _tmpText.color = new Color(_startColor.r, _startColor.g, _startColor.b, a);

        if (_timer >= lifetime)
            Destroy(gameObject);
    }
}
