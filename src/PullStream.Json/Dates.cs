namespace PullStream.Json
{
    using Newtonsoft.Json;

    public sealed class Dates : Configuration
    {
        private readonly string format;
        private readonly DateTimeZoneHandling timeZoneHandling;

        public Dates(string format, DateTimeZoneHandling timeZoneHandling = DateTimeZoneHandling.RoundtripKind)
        {
            this.format = format;
            this.timeZoneHandling = timeZoneHandling;
        }

        public override void Apply(JsonTextWriter target)
        {
            target.DateFormatString = format;
            target.DateTimeZoneHandling = timeZoneHandling;
        }
    }
}