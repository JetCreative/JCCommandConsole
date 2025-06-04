using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

namespace JetCreative.Serialization
{
    [System.Serializable]
    public class SerializedPropertyInfo: ISerializationCallbackReceiver
    {
        public SerializedPropertyInfo(PropertyInfo aPropertyInfo)
        {
            PropertyInfo = aPropertyInfo;
        }
        
        public PropertyInfo PropertyInfo;
        public SerializableType Type;
        public string PropertyName;
        public int Flags = 0;
        
        
        public void OnBeforeSerialize()
        {
            if (PropertyInfo == null)
                return;

            Type = new SerializableType(PropertyInfo.DeclaringType);
            PropertyName = PropertyInfo.Name;
            
            var getMethod = PropertyInfo.GetGetMethod();
            var setMethod = PropertyInfo.GetSetMethod();
            
            if ((getMethod != null && getMethod.IsPrivate) 
                || (setMethod != null && setMethod.IsPrivate))
                Flags |= (int)BindingFlags.NonPublic;
            else
                Flags |= (int)BindingFlags.Public;
            
            
            if ((getMethod != null && getMethod.IsStatic) 
                || (setMethod != null && setMethod.IsStatic ))
                Flags |= (int)BindingFlags.Static;
            else
                Flags |= (int)BindingFlags.Instance;
        }

        public void OnAfterDeserialize()
        {
            if (Type == null || string.IsNullOrEmpty(PropertyName))
                return;
            var t = Type.Type;
            PropertyInfo = t.GetProperty(PropertyName, (BindingFlags)Flags);
        }
    }
}