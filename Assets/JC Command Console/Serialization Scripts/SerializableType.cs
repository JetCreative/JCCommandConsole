//https://discussions.unity.com/t/saved-methodinfo-variable-resets-on-compile/161533/3

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace JetCreative.Serialization
{
        [Serializable]

    public class SerializableType : ISerializationCallbackReceiver
    {
        [NonSerialized] public Type Type;
        public byte[] Data;

        public SerializableType(Type aType)
        {
            Type = aType;
        }

        public static Type Read(BinaryReader aReader)
        {
            var paramCount = aReader.ReadByte();
            if (paramCount == 0xFF)
                return null;

            var typeName = aReader.ReadString();
            var type = Type.GetType(typeName);
            if (type == null)
                throw new Exception("Can't find type; '" + typeName + "'");

            if (type.IsGenericTypeDefinition && paramCount > 0)
            {
                var p = new Type[paramCount];

                for (int i = 0; i < paramCount; i++)
                {
                    p[i] = Read(aReader);
                }

                type = type.MakeGenericType(p);
            }

            return type;
        }

        public static void Write(BinaryWriter aWriter, Type aType)
        {
            if (aType == null)
            {
                aWriter.Write((byte)0xFF);
                return;
            }

            if (aType.IsGenericType)
            {
                var t = aType.GetGenericTypeDefinition();
                var p = aType.GetGenericArguments();
                aWriter.Write((byte)p.Length);
                aWriter.Write(t.AssemblyQualifiedName);

                for (int i = 0; i < p.Length; i++)
                {
                    Write(aWriter, p[i]);
                }

                return;
            }

            aWriter.Write((byte)0);
            aWriter.Write(aType.AssemblyQualifiedName);
        }


        public void OnBeforeSerialize()
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                Write(w, Type);
                Data = stream.ToArray();
            }
        }

        public void OnAfterDeserialize()
        {
            using (var stream = new MemoryStream(Data))
            using (var r = new BinaryReader(stream))
            {
                Type = Read(r);
            }
        }
    }
}
