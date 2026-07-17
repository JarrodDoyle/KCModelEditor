using System.Text.Json.Serialization;

namespace KeepersCompound.ModelEditor.UI.About;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(LicenseMeta))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext;