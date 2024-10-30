using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    public TValue this[TKey key]
    {
        get
        {
            int index = keys.IndexOf(key);
            if (index >= 0) return values[index];
            throw new KeyNotFoundException($"Key {key} not found in dictionary");
        }
        set
        {
            int index = keys.IndexOf(key);
            if (index >= 0)
                values[index] = value;
            else
            {
                keys.Add(key);
                values.Add(value);
            }
        }
    }

    public bool ContainsKey(TKey key) => keys.Contains(key);
    public int Count => keys.Count;
}