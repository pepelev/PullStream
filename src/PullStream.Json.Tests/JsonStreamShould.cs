namespace PullStream.Json.Tests
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using FluentAssertions;
    using NUnit.Framework;

    public sealed class JsonStreamShould
    {
        [Test]
        public void Contain_Numbers()
        {
            using var stream = JsonStream.ArrayOf(new[] {10, 20, 40}).Build();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            json.Should().Be("[10,20,40]");
        }

        [Test]
        public void Contain_Objects()
        {
            var pets = new[]
            {
                new
                {
                    Name = "Bella",
                    Race = "Cat",
                    Age = 3
                },
                new
                {
                    Name = "Max",
                    Race = "Dog",
                    Age = 7
                }
            };
            using var stream = JsonStream.ArrayOf(pets).Build();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            json.Should().Be(@"[{""Name"":""Bella"",""Race"":""Cat"",""Age"":3},{""Name"":""Max"",""Race"":""Dog"",""Age"":7}]");
        }

        [Test]
        public void Contain_Async()
        {
            using var stream = JsonStream.ArrayOf(new[] { 10, 20, 40 }.ToAsyncEnumerable()).Build();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            json.Should().Be("[10,20,40]");
        }

        [Test]
        public void Use_Serializer_Settings()
        {
            var enumerable = new[] { 72, 17, 50, 4 };
            using var stream = JsonStream
                .Using(
                    new JsonTextWriterFactory(
                        1024,
                        new Indented()
                    )
                )
                .ArrayOf(enumerable)
                .Build();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            json.Should().Be(@"[
  72,
  17,
  50,
  4
]");
        }
    }
}