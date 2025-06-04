using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

namespace JetCreative.Serialization
{
    [System.Serializable]

    public class SerializedFieldInfo : ISerializationCallbackReceiver
    {
        public SerializedFieldInfo(FieldInfo aFieldInfo)
        {
            FieldInfo = aFieldInfo;
        }

        public FieldInfo FieldInfo;
        public SerializableType Type;
        public string FieldName;
        public int Flags = 0;

        public void OnBeforeSerialize()
        {
            if (FieldInfo == null)
                return;

            Type = new SerializableType(FieldInfo.DeclaringType);
            FieldName = FieldInfo.Name;
            if (FieldInfo.IsPrivate)
                Flags |= (int)BindingFlags.NonPublic;
            else
                Flags |= (int)BindingFlags.Public;
            if (FieldInfo.IsStatic)
                Flags |= (int)BindingFlags.Static;
            else
                Flags |= (int)BindingFlags.Instance;

        }

        public void OnAfterDeserialize()
        {
            if (Type == null || string.IsNullOrEmpty(FieldName))
                return;

            var t = Type.Type;

            FieldInfo = t.GetField(FieldName, (BindingFlags)Flags);

        }
    }
}
