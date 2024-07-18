namespace HomeworkApp.Dal.Exceptions;

public class CyclicDependenceException : Exception
{
    public CyclicDependenceException(long id) : base()
    {
        Id = id;
    }
    public long Id { get; private init; }
    
    public override string Message
    {
        get => $"Task Id = {Id} cannot match parent task Id";
    }
}