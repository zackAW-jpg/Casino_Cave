using UnityEngine;

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
