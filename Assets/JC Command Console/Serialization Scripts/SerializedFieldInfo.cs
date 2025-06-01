using System.Reflection;
using UnityEngine;

namespace JetCreative.Serialization
{
    [System.Serializable]

    public class SerializedFieldInfo : ISerializationCallbackReceiver
    {
        public SerializedFieldInfo(FieldInfo aFieldInfo)
        {
            fieldInfo = aFieldInfo;
        }

        public FieldInfo fieldInfo;
        public SerializableType type;
        public string fieldName;
        public int flags = 0;

        public void OnBeforeSerialize()
        {
            if (fieldInfo == null)
                return;

            type = new SerializableType(fieldInfo.DeclaringType);
            fieldName = fieldInfo.Name;
            if (fieldInfo.IsPrivate)
                flags |= (int)BindingFlags.NonPublic;
            else
                flags |= (int)BindingFlags.Public;
            if (fieldInfo.IsStatic)
                flags |= (int)BindingFlags.Static;
            else
                flags |= (int)BindingFlags.Instance;

        }

        public void OnAfterDeserialize()
        {
            if (type == null || string.IsNullOrEmpty(fieldName))
                return;

            var t = type.type;

            fieldInfo = t.GetField(fieldName, (BindingFlags)flags);

        }
    }
}
