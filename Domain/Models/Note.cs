using System;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Pika.Domain.Notes.Data;

public class Note
{
    private string _type = NoteType.Note.ToSerializedValue();

    [JsonProperty(PropertyName = "id")]
    [JsonPropertyName("id")]
    public string Id { get; private set; } = string.Empty;

    [JsonProperty(PropertyName = "humanName")]
    [JsonPropertyName("humanName")]
    public string HumanName { get; private set; } = string.Empty;

    [JsonProperty(PropertyName = "machineName")]
    [JsonPropertyName("machineName")]
    public string MachineName { get; private set; } = string.Empty;

    [JsonProperty(PropertyName = "bucketId")]
    [JsonPropertyName("bucketId")]
    public string BucketId { get; private set; } = string.Empty;

    [JsonProperty(PropertyName = "timestamp")]
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    [JsonProperty(PropertyName = "type")]
    [JsonPropertyName("type")]
    public string Type
    {
        get => _type;
        private set => _type = string.IsNullOrWhiteSpace(value) ? NoteType.Note.ToSerializedValue() : value;
    }

    public void AssignId(string id)
    {
        Id = id;
    }

    public void GenerateId()
    {
        Id = Guid.NewGuid().ToString();
    }

    public void UpdateHumanName(string humanName)
    {
        HumanName = humanName;
    }

    public void UpdateMachineName(string machineName)
    {
        MachineName = machineName;
    }

    public void UpdateBucketId(string bucketId)
    {
        BucketId = bucketId;
    }

    public void UpdateType(NoteType noteType)
    {
        Type = noteType.ToSerializedValue();
    }

    public NoteType GetNoteType()
    {
        return NoteTypeExtensions.FromSerializedValue(Type);
    }
}
