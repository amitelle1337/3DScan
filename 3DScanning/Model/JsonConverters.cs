using Intel.RealSense;
using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _3DScanning
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var arr = new float[3];

            _ = reader.Read(); // Read start object
            for (var i = 0; i < arr.Length; ++i)
            {
                _ = reader.GetString(); // Read propery name, add checks [?]
                _ = reader.Read(); // Read ':'
                arr[i] = reader.GetSingle(); // Read value
                _ = reader.Read(); // Read the ',' and at last read the end object
            }
            return new Vector3(arr[0], arr[1], arr[2]);
        }

        public override void Write(Utf8JsonWriter writer, Vector3 vec, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", vec.X);
            writer.WriteNumber("Y", vec.Y);
            writer.WriteNumber("Z", vec.Z);
            writer.WriteEndObject();
        }
    }

    public class ProcessingBlockConverter : JsonConverter<ProcessingBlock>
    {
        public override ProcessingBlock Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ProcessingBlock block = default;
            _ = reader.Read(); // Read start object
            _ = reader.GetString(); // Read "Name", add checks [?]
            _ = reader.Read(); // Read the ':'
            var name = reader.GetString(); // Read the name
            switch (name)
            {
                case "Decimation Filter":
                    block = new DecimationFilter();
                    _ = reader.Read(); // Read the ','
                    _ = reader.GetString(); // Read "FilterMagnitude", add checks [?]
                    _ = reader.Read(); // Read the ':'
                    block.Options[Option.FilterMagnitude].Value = reader.GetSingle();
                    _ = reader.Read(); // Read end object
                    break;
                case "Spatial Filter":
                    block = new SpatialFilter();
                    _ = reader.Read(); // Read the ','
                    _ = reader.GetString(); // Read "FilterMagnitude", add checks [?]
                    _ = reader.Read(); // Read the ':'
                    block.Options[Option.FilterMagnitude].Value = reader.GetSingle();
                    _ = reader.Read(); // Read the ','
                    _ = reader.GetString(); // Read "FilterSmoothAlpha", add checks [?]
                    _ = reader.Read(); // Read the ':'
                    block.Options[Option.FilterSmoothAlpha].Value = reader.GetSingle();
                    _ = reader.Read(); // Read the ','
                    _ = reader.GetString(); // Read "FilterSmoothDelta", add checks [?]
                    _ = reader.Read(); // Read the ':'
                    block.Options[Option.FilterSmoothDelta].Value = reader.GetSingle();
                    _ = reader.Read(); // Read end object
                    break;
                case "Temporal Filter":
                    block = new TemporalFilter();
                    _ = reader.Read(); // Read the ','
                    _ = reader.GetString(); // Read "FilterSmoothAlpha", add checks [?]
                    _ = reader.Read(); // Read the ':'
                    block.Options[Option.FilterSmoothAlpha].Value = reader.GetSingle();
                    _ = reader.Read(); // Read the ','
                    _ = reader.GetString(); // Read "FilterSmoothDelta", add checks [?]
                    _ = reader.Read(); // Read the ':'
                    block.Options[Option.FilterSmoothDelta].Value = reader.GetSingle();
                    _ = reader.Read(); // Read end object
                    break;
                case "Hole Filling Filter":
                    block = new HoleFillingFilter();
                    _ = reader.Read(); // Read the ','
                    _ = reader.GetString(); // Read "HolesFill", add checks [?]
                    _ = reader.Read(); // Read the ':'
                    block.Options[Option.HolesFill].Value = reader.GetSingle();
                    _ = reader.Read(); // Read end object
                    break;
                case "Threshold Filter":
                    block = new ThresholdFilter();
                    _ = reader.Read(); // Read the ','
                    _ = reader.GetString(); // Read "MinDistance", add checks [?]
                    _ = reader.Read(); // Read the ':'
                    block.Options[Option.MinDistance].Value = reader.GetSingle();
                    _ = reader.Read(); // Read the ','
                    _ = reader.GetString(); // Read "MaxDistance", add checks [?]
                    _ = reader.Read(); // Read the ':'
                    block.Options[Option.MaxDistance].Value = reader.GetSingle();
                    _ = reader.Read(); // Read end object
                    break;
                default:
                    throw new NotSupportedException($"The filter {name} is not supported in this converter");
            }
            return block;
        }

        public override void Write(Utf8JsonWriter writer, ProcessingBlock block, JsonSerializerOptions options)
        {
            var name = block.Info[CameraInfo.Name];
            switch (name)
            {
                case "Decimation Filter":
                    writer.WriteStartObject();
                    writer.WriteString("Name", name);
                    writer.WriteNumber("FilterMagnitude", block.Options[Option.FilterMagnitude].Value);
                    writer.WriteEndObject();
                    break;
                case "Spatial Filter":
                    writer.WriteStartObject();
                    writer.WriteString("Name", name);
                    writer.WriteNumber("FilterMagnitude", block.Options[Option.FilterMagnitude].Value);
                    writer.WriteNumber("FilterSmoothAlpha", block.Options[Option.FilterSmoothAlpha].Value);
                    writer.WriteNumber("FilterSmoothDelta", block.Options[Option.FilterSmoothDelta].Value);
                    writer.WriteEndObject();
                    break;
                case "Temporal Filter":
                    writer.WriteStartObject();
                    writer.WriteString("Name", name);
                    writer.WriteNumber("FilterSmoothAlpha", block.Options[Option.FilterSmoothAlpha].Value);
                    writer.WriteNumber("FilterSmoothDelta", block.Options[Option.FilterSmoothDelta].Value);
                    writer.WriteEndObject();
                    break;
                case "Hole Filling Filter":
                    writer.WriteStartObject();
                    writer.WriteString("Name", name);
                    writer.WriteNumber("HolesFill", block.Options[Option.HolesFill].Value);
                    writer.WriteEndObject();
                    break;
                case "Threshold Filter":
                    writer.WriteStartObject();
                    writer.WriteString("Name", name);
                    writer.WriteNumber("MinDistance", block.Options[Option.MinDistance].Value);
                    writer.WriteNumber("MaxDistance", block.Options[Option.MaxDistance].Value);
                    writer.WriteEndObject();
                    break;
                default:
                    throw new NotSupportedException($"The filter {name} is not supported in this converter");
            }
        }
    }
}