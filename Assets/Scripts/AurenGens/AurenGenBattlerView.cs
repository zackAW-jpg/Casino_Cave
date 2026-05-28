using UnityEngine;

[DisallowMultipleComponent]
public class AurenGenBattlerView : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Sprite renderer for this battler.")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Animator for this battler.")]
    public Animator animatorRef;

    [Header("Animation triggers")]
    [Tooltip("Trigger for attack anim.")]
    public string attackTrigger = "Attack";

    [Tooltip("Trigger for special anim.")]
    public string specialTrigger = "Special";

    [Tooltip("Trigger for hit anim.")]
    public string hitTrigger = "Hit";

    [Tooltip("Trigger for break anim.")]
    public string breakTrigger = "Break";

    [Tooltip("Trigger for heal / buff anim.")]
    public string healTrigger = "Heal";

    void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animatorRef = GetComponentInChildren<Animator>();
    }

    public void Bind(AurenGenDefinition def, bool asPlayerSide)
    {
        if (def == null)
            return;

        if (spriteRenderer != null)
            spriteRenderer.sprite = asPlayerSide ? def.backSprite : def.frontSprite;

        if (animatorRef != null)
            animatorRef.runtimeAnimatorController = asPlayerSide ? def.backAnimatorController : def.frontAnimatorController;
    }

    public void PlayAttack(bool special)
    {
        if (animatorRef == null)
            return;
        if (special && !string.IsNullOrEmpty(specialTrigger))
            animatorRef.SetTrigger(specialTrigger);
        else if (!string.IsNullOrEmpty(attackTrigger))
            animatorRef.SetTrigger(attackTrigger);
    }

    public void PlayHit()
    {
        if (animatorRef == null || string.IsNullOrEmpty(hitTrigger))
            return;
        animatorRef.SetTrigger(hitTrigger);
    }

    public void PlayBreak()
    {
        if (animatorRef == null || string.IsNullOrEmpty(breakTrigger))
            return;
        animatorRef.SetTrigger(breakTrigger);
    }

    public void PlayHeal()
    {
        if (animatorRef == null || string.IsNullOrEmpty(healTrigger))
            return;
        animatorRef.SetTrigger(healTrigger);
    }
}
