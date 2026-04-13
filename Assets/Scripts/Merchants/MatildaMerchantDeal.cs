using UnityEngine;

public class MatildaMerchantDeal : MonoBehaviour, INpcDealDialogueHandler
{
    [Header("Matilda Dialogue")]
    [TextArea(3, 6)]
    public string line1 = "hello lucky munchkin, i am matilda! ra-ta-ta-ta!!!";
    [TextArea(3, 6)]
    public string line2 = "i have a deal! a chance for the *exchange* in blood!!! do ye partake???";

    public string GetDialogueLine1() => line1;
    public string GetDialogueLine2() => line2;

    public string[] GetDialogueLines()
    {
        return new[] { line1, line2 };
    }

    public void OnDealYes(PlayerHealth player, out string resultText)
    {
        if (player == null || player.state == null)
        {
            resultText = "The deal fizzles out... (player missing).";
            return;
        }

        int third = Mathf.Max(1, player.state.maxHP / 3);

        float roll = Random.value;

        if (roll < 1f / 3f)
        {
            player.TakeDamage(third);
            resultText = $"Oh no! The exchange in blood costs you {third} HP!";
        }
        else if (roll < 2f / 3f)
        {
            resultText = "The roulette wheel spins... and you get nothing this time.";
        }
        else
        {
            player.HealWithTempOverflow(third);
            resultText = $"Lucky! You gain {third} HP (temporary overflow possible).";
        }
    }

    public void OnDealNo(PlayerHealth player, out string resultText)
    {
        resultText = "Matilda twirls her baton... maybe next time, munchkin.";
    }
}
