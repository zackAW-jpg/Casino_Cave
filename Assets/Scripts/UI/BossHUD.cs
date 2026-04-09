using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss HP bar (bottom-center). Call <see cref="Bind"/> when a boss spawns; hides on boss death.
/// Reuses the same fill-Image pattern as <see cref="PlayerHUD"/>.
/// </summary>
public class BossHUD : MonoBehaviour
{
    [Header("UI")]
    public Image hpFillImage;
    [Tooltip("Optional. Name is taken from BossHealth.displayName (e.g. \"The Burnt One\"). Place to the left of the bar.")]
    public TextMeshProUGUI bossNameText;
    [Tooltip("Optional extra root under this object (e.g. HPBar). Bind always enables this component's GameObject first so a disabled parent does not hide the bar.")]
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

    /// <summary>Wire this boss to the bar and show the HUD.</summary>
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

        // Parent must be active or children stay hidden even if hudRoot.SetActive(true) is called.
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
        bossNameText.text = string.IsNullOrEmpty(_boss.displayName) ? "The Burnt One" : _boss.displayName;
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
