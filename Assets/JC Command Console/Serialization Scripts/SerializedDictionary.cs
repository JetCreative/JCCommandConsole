using System.Collections.Generic;
using UnityEngine;

namespace JetCreative.Serialization
{

    /// <summary>
    /// Serialize dictionaries by creating new types inheriting from this class. 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>

    public abstract class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {

        [SerializeField, HideInInspector] private List<TKey> keyData = new List<TKey>();

        [SerializeField, HideInInspector] private List<TValue> valueData = new List<TValue>();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();

            for (int i = 0; i < keyData.Count && i < valueData.Count; i++)
            {
                this[keyData[i]] = valueData[i];
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            keyData.Clear();
            valueData.Clear();

            foreach (var item in this)
            {
                keyData.Add(item.Key);
                valueData.Add(item.Value);
            }
        }


        //Example how to create new dictionary type
        //
        // [Serializable] public class ExampleDictionary : UnitySerializedDictionary<Key, Value> { }
    }
}