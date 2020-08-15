using Intel.RealSense;
using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _3DScanning.Model
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var arr = new float[3];

            _ = reader.Read(); // Read start object
            for (var i = 0; i < arr.Length; ++i)
            {
                Console.WriteLine(reader.GetString()); // Reader propery name
                Console.WriteLine(reader.Read()); // Reader ':'
                arr[i] = reader.GetSingle();
                Console.WriteLine(reader.Read()); // Read the ',' and at last read the end object
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
            var name = reader.GetString();
            ProcessingBlock block = default;
            switch (name)
            {
                case "Decimation filter":
                    block = new DecimationFilter();
                    block.Options[Option.FilterMagnitude].Value = reader.GetSingle();
                    break;
                case "Spatial Filter":
                    block = new DecimationFilter();
                    block.Options[Option.FilterMagnitude].Value = reader.GetSingle();
                    block.Options[Option.FilterSmoothAlpha].Value = reader.GetSingle();
                    block.Options[Option.FilterSmoothDelta].Value = reader.GetSingle();
                    break;
                case "Temporal filter":
                    block = new TemporalFilter();
                    block.Options[Option.FilterSmoothAlpha].Value = reader.GetSingle();
                    block.Options[Option.FilterSmoothDelta].Value = reader.GetSingle();
                    break;
                case "Hole Filling Filter":
                    block = new HoleFillingFilter();
                    block.Options[Option.HolesFill].Value = reader.GetSingle();
                    break;
                case "Threshold Filter":
                    block = new ThresholdFilter();
                    block.Options[Option.MinDistance].Value = reader.GetSingle();
                    block.Options[Option.MaxDistance].Value = reader.GetSingle();
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
                case "Decimation filter":
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
                case "Temporal filter":
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