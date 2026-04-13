using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MerchantSpawnTable", menuName = "Game Data/Merchant Spawn Table")]
public class MerchantSpawnTableSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public GameObject merchantPrefab;
        [Min(0f)]
        public float weight = 1f;
    }

    public List<Entry> entries = new List<Entry>();

    public GameObject PickPrefab()
    {
        float total = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] == null) continue;
            if (entries[i].merchantPrefab == null) continue;
            if (entries[i].weight <= 0f) continue;

            total += entries[i].weight;
        }

        if (total <= 0f)
            return null;

        float r = Random.value * total;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] == null) continue;
            if (entries[i].merchantPrefab == null) continue;
            if (entries[i].weight <= 0f) continue;

            r -= entries[i].weight;
            if (r <= 0f)
                return entries[i].merchantPrefab;
        }

        return null;
    }
}
