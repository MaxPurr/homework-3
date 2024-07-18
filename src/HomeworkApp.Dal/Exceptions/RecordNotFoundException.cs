using System;

namespace HomeworkApp.Dal.Exceptions;

public class RecordNotFoundException : Exception
{
    public RecordNotFoundException(long id) : base()
    {
        Id = id;
    }
    public long Id { get; private init; }

    public override string Message
    {
        get => $"Record with id = {Id} is not found";
    }
}