using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Jellyfin.UWP.Exceptions;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
[Serializable]
public sealed class ItemNotFoundException : Exception
{
    public ItemNotFoundException()
    {
    }

    public ItemNotFoundException(string? message) : base(message)
    {
    }

    private ItemNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public ItemNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}
