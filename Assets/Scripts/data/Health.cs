using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHP = 5;
    public int currentHP;

    [Header("Optional: floating damage number")]
    public GameObject damagePopupPrefab;
    public Vector3 popupOffset = new Vector3(0f, 0.2f, 0f);

    [Header("Audio (optional — grunts / generic enemies)")]
    [Tooltip("Plays when this enemy takes damage (e.g. assign grunt hit SFX on the Security Guard prefab).")]
    public AudioSource hurtAudioSource;
    public AudioClip hurtSound;
    public AudioClip deathSound;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHP -= amount;

        if (damagePopupPrefab != null)
        {
            Vector3 pos = transform.position + popupOffset;
            GameObject popup = Object.Instantiate(damagePopupPrefab, pos, Quaternion.identity);
            Canvas c = popup.GetComponent<Canvas>();
            if (c != null && c.renderMode == RenderMode.WorldSpace && Camera.main != null)
                c.worldCamera = Camera.main;
            DamagePopup dp = popup.GetComponent<DamagePopup>();
            if (dp != null)
                dp.SetAmount(amount);
        }

        Debug.Log($"{name} took {amount} damage, HP now {currentHP}");

        if (hurtAudioSource == null)
            hurtAudioSource = GetComponent<AudioSource>();
        SfxUtil.PlayOneShot(hurtSound, hurtAudioSource, transform.position);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died");

        if (hurtAudioSource == null)
            hurtAudioSource = GetComponent<AudioSource>();
        SfxUtil.PlayOneShot(deathSound, hurtAudioSource, transform.position);

        Destroy(gameObject);
    }
}