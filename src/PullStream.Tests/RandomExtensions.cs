using System;

namespace PullStream.Tests
{
    public static class RandomExtensions
    {
        public static byte[] NextBytes(this Random random, int minLength, int maxLength)
        {
            var length = random.Next(minLength, maxLength + 1);
            var result = new byte[length];
            random.NextBytes(result);
            return result;
        }
    }
}