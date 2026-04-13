using UnityEngine;

/// <summary>
/// Place in the boss room (spawner assigns when the boss is defeated). Uses a trigger collider on the Player.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CaveExitBeacon : MonoBehaviour
{
    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (!GameplayInputGate.PlayerWorldActionsEnabled)
            return;

        GameplayMenusController.Instance?.ShowLeaveCavePrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        GameplayMenusController.Instance?.HideLeaveCavePrompt();
    }
}
