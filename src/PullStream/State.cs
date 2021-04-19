namespace PullStream
{
    internal enum State
    {
        MoveNext,
        Current,
        Cleanup,
        Completed,
        Disposed
    }
}