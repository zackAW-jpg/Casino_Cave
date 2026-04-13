using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoinPickup : MonoBehaviour
{
    public int goldAmount = 1;

    [Header("Look (pickups)")]
    public Sprite chipSpriteRed;

    public Sprite chipSpriteBlack;

    private void Awake()
    {
        
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

        GameplaySaveContext.PersistRun();

        Destroy(gameObject);
    }
}
