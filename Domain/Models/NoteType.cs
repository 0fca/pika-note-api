using System;

namespace Pika.Domain.Notes.Data;

public enum NoteType
{
    Note,
    Sheet
}

public static class NoteTypeExtensions
{
    public static string ToSerializedValue(this NoteType noteType)
    {
        return noteType switch
        {
            NoteType.Note => "note",
            NoteType.Sheet => "sheet",
            _ => throw new ArgumentOutOfRangeException(nameof(noteType), noteType, null)
        };
    }

    public static NoteType FromSerializedValue(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            null or "" => NoteType.Note,
            "note" => NoteType.Note,
            "sheet" => NoteType.Sheet,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported note type.")
        };
    }
}
