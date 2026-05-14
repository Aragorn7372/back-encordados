namespace BackEncordados.Common.Database.Helpers;


public interface ITimestamped
{
    DateTime CreatedAt { get; }

    DateTime UpdatedAt { get; }
}