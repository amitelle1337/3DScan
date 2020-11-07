using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _3DScan.Model
{
    /// <inheritdoc/>
    public class Vector3Converter : JsonConverter<Vector3>
    {
        /// <inheritdoc/>
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var arr = new float[3];

            //Note: Maybe we should add checks for each read.

            _ = reader.Read(); // Read start object
            for (var i = 0; i < arr.Length; ++i)
            {
                _ = reader.GetString(); // Read property name.
                _ = reader.Read(); // Read ':'.
                arr[i] = reader.GetSingle(); // Read value.
                _ = reader.Read(); // Read the ',' and at last read the end object.
            }
            return new Vector3(arr[0], arr[1], arr[2]);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Vector3 vec, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", vec.X);
            writer.WriteNumber("Y", vec.Y);
            writer.WriteNumber("Z", vec.Z);
            writer.WriteEndObject();
        }
    }
}