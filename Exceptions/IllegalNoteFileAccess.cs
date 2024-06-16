using System;

namespace PikaNoteAPI.Exceptions;

public class IllegalNoteFileAccess : Exception
{
    public IllegalNoteFileAccess() : base("Illegal access to a note metadata.")
    {
    }
}