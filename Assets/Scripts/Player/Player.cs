using UnityEngine;

public class Player :MonoBehaviour
{
    public PlayerStateSO state;

    private void Start()
    {
        Debug.Log("Chips: " + state.gold);
    }
}
