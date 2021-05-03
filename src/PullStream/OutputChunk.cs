namespace PullStream
{
    public abstract class OutputChunk<T>
    {
        public abstract void Write(T output);
    }
}