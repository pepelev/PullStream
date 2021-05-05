namespace PullStream.Json
{
    using Newtonsoft.Json;

    public abstract class Configuration
    {
        public static Configuration Default => new DefaultConfiguration();

        public abstract void Apply(JsonTextWriter target);

        private sealed class DefaultConfiguration : Configuration
        {
            public override void Apply(JsonTextWriter target)
            {
                // intentionally left blank
            }
        }

        public sealed class Composite : Configuration
        {
            private readonly Configuration[] parts;

            public Composite(params Configuration[] parts)
            {
                this.parts = parts;
            }

            public override void Apply(JsonTextWriter target)
            {
                foreach (var part in parts)
                {
                    part.Apply(target);
                }
            }
        }
    }
}