using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoinPickup : MonoBehaviour
{
    public int goldAmount = 1;

    [Header("Look (pickups)")]
    [Tooltip("If both are set, the chip uses one or the other at random (50/50). Same value in player state either way.")]
    public Sprite chipSpriteRed;

    public Sprite chipSpriteBlack;

    private void Awake()
    {
        // Make sure this collider is a trigger so the player can walk through it
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        ApplyRandomChipVisual();
    }

    private void ApplyRandomChipVisual()
    {
        if (chipSpriteRed == null && chipSpriteBlack == null)
            return;

        if (!TryGetComponent<SpriteRenderer>(out var sr))
            return;

        Sprite chosen;
        if (chipSpriteRed != null && chipSpriteBlack != null)
            chosen = Random.value < 0.5f ? chipSpriteRed : chipSpriteBlack;
        else
            chosen = chipSpriteRed != null ? chipSpriteRed : chipSpriteBlack;

        sr.sprite = chosen;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null || player.state == null)
            return;

        player.state.gold += goldAmount;
        Debug.Log($"You've gained {goldAmount} chip(s). Total chips: {player.state.gold}");

        Destroy(gameObject);
    }
}