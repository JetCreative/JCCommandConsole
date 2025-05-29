using System.Reflection;
using UnityEngine;

namespace JetCreative.Serialization
{
    [System.Serializable]
    public class SerializedPropertyInfo: ISerializationCallbackReceiver
    {
        public SerializedPropertyInfo(PropertyInfo aPropertyInfo)
        {
            propertyInfo = aPropertyInfo;
        }
        
        public PropertyInfo propertyInfo;
        public SerializableType type;
        public string propertyName;
        public int flags = 0;
        
        
        public void OnBeforeSerialize()
        {
            if (propertyInfo == null)
                return;

            type = new SerializableType(propertyInfo.DeclaringType);
            propertyName = propertyInfo.Name;
            
            var getMethod = propertyInfo.GetGetMethod();
            var setMethod = propertyInfo.GetSetMethod();
            
            if ((getMethod != null && getMethod.IsPrivate) 
                || (setMethod != null && setMethod.IsPrivate))
                flags |= (int)BindingFlags.NonPublic;
            else
                flags |= (int)BindingFlags.Public;
            
            
            if ((getMethod != null && getMethod.IsStatic) 
                || (setMethod != null && setMethod.IsStatic ))
                flags |= (int)BindingFlags.Static;
            else
                flags |= (int)BindingFlags.Instance;
        }

        public void OnAfterDeserialize()
        {
            if (type == null || string.IsNullOrEmpty(propertyName))
                return;
            var t = type.type;
            propertyInfo = t.GetProperty(propertyName, (BindingFlags)flags);
        }
    }
}