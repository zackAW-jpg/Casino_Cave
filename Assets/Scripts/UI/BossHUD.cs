using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHUD : MonoBehaviour
{
    [Header("UI")]
    public Image hpFillImage;
    public TextMeshProUGUI bossNameText;
    public GameObject hudRoot;

    [Header("Fill smoothing")]
    public float hpFillLerpDuration = 0.2f;

    private BossHealth _boss;
    private float _displayedHp;

    private void Update()
    {
        if (_boss == null || hpFillImage == null || _boss.maxHP <= 0)
            return;

        float target = _boss.currentHP;
        float hpPerSecond = _boss.maxHP / Mathf.Max(0.01f, hpFillLerpDuration);
        _displayedHp = Mathf.MoveTowards(_displayedHp, target, hpPerSecond * Time.deltaTime);
        hpFillImage.fillAmount = Mathf.Clamp01(_displayedHp / _boss.maxHP);
    }

    
    public void Bind(BossHealth boss)
    {
        Unbind();

        _boss = boss;
        if (_boss == null)
            return;

        if (hpFillImage != null && hpFillImage.type != Image.Type.Filled)
            Debug.LogWarning("BossHUD: Set hpFillImage Image Type to Filled (horizontal) so fillAmount updates the bar.", hpFillImage);

        _boss.OnDeath += HandleBossDeath;

        _displayedHp = _boss.currentHP;
        ApplyImmediate();
        ApplyBossName();

        
        gameObject.SetActive(true);
        if (hudRoot != null)
            hudRoot.SetActive(true);
    }

    public void Unbind()
    {
        if (_boss != null)
        {
            _boss.OnDeath -= HandleBossDeath;
            _boss = null;
        }

        if (hudRoot != null)
            hudRoot.SetActive(false);
        gameObject.SetActive(false);
    }

    private void HandleBossDeath()
    {
        Unbind();
    }

    private void ApplyImmediate()
    {
        if (hpFillImage == null || _boss == null || _boss.maxHP <= 0) return;
        hpFillImage.fillAmount = Mathf.Clamp01((float)_boss.currentHP / _boss.maxHP);
    }

    private void ApplyBossName()
    {
        if (bossNameText == null || _boss == null)
            return;
        bossNameText.text = string.IsNullOrEmpty(_boss.displayName) ? "The Awakened One" : _boss.displayName;
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
