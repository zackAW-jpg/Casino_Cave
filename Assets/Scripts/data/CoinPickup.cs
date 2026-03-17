using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoinPickup : MonoBehaviour
{
    public int goldAmount = 1;

    private void Awake()
    {
        // Make sure this collider is a trigger so the player can walk through it
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null || player.state == null)
            return;

        player.state.gold += goldAmount;
        Debug.Log($"You've gained {goldAmount} gold! Total gold: {player.state.gold}");

        Destroy(gameObject);
    }
}