using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WeightedRandomSelector
{
    public static T Choose<T>(IEnumerable<(T item, float weight)> options)
    {
        float totalWeight = options.Sum(opt => opt.weight);
        if (totalWeight <= 0f)
        {
            Debug.LogWarning("WeightedRandomSelector: Total weight is zero or negative.");
            return default;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var (item, weight) in options)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return item;
        }

        Debug.LogWarning("WeightedRandomSelector: No valid selection made. Returning default.");
        return default;
    }
}