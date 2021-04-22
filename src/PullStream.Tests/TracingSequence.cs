using System.Collections;
using System.Collections.Generic;

namespace PullStream.Tests
{
    public sealed class TracingSequence<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> sequence;
        public TracingEnumerator<T> Enumerator { get; } = new();

        public TracingSequence(IEnumerable<T> sequence)
        {
            this.sequence = sequence;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var enumerator = sequence.GetEnumerator();
            Enumerator.Inner = enumerator;
            EnumeratorRequested = true;
            return Enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool EnumeratorRequested { get; private set; }
    }
}