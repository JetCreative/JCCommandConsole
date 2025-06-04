//https://discussions.unity.com/t/saved-methodinfo-variable-resets-on-compile/161533/3

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

namespace JetCreative.Serialization
{
    
    [Serializable]

    public class SerializableMethodInfo : ISerializationCallbackReceiver
    {
        public SerializableMethodInfo(MethodInfo aMethodInfo)
        {
            MethodInfo = aMethodInfo;
        }

        public MethodInfo MethodInfo;
        public SerializableType Type;
        public string MethodName;
        public List<SerializableType> Parameters = null;
        public int Flags = 0;

        public void OnBeforeSerialize()
        {
            if (MethodInfo == null)
                return;

            Type = new SerializableType(MethodInfo.DeclaringType);
            MethodName = MethodInfo.Name;
            if (MethodInfo.IsPrivate)
                Flags |= (int)BindingFlags.NonPublic;
            else
                Flags |= (int)BindingFlags.Public;
            if (MethodInfo.IsStatic)
                Flags |= (int)BindingFlags.Static;
            else
                Flags |= (int)BindingFlags.Instance;
            var p = MethodInfo.GetParameters();

            if (p != null && p.Length > 0)
            {
                Parameters = new List<SerializableType>(p.Length);

                for (int i = 0; i < p.Length; i++)
                {
                    Parameters.Add(new SerializableType(p[i].ParameterType));
                }
            }
            else
                Parameters = null;
        }

        public void OnAfterDeserialize()
        {
            if (Type == null || string.IsNullOrEmpty(MethodName))
                return;

            var t = Type.Type;
            Type[] param = null;

            if (Parameters != null && Parameters.Count > 0)
            {
                param = new Type[Parameters.Count];

                for (int i = 0; i < Parameters.Count; i++)
                {
                    param[i] = Parameters[i].Type;
                }
            }

            if (param == null)
                MethodInfo = t.GetMethod(MethodName, (BindingFlags)Flags);
            else
                MethodInfo = t.GetMethod(MethodName, (BindingFlags)Flags, null, param, null);
        }
    }

}