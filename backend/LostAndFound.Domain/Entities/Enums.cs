namespace LostAndFound.Domain.Entities;

public enum ItemStatus
{
    Received = 0,
    InStorage = 1,
    Transferred = 2,
    Claimed = 3,
    Disposed = 4,
    ReadyToDispose = 5,
    Destroyed = 6,
    InTransit = 7,
    Sold = 8
}
