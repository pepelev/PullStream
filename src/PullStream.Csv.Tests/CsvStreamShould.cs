using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace PullStream.Csv.Tests
{
    public sealed class CsvStreamShould
    {
        [Test]
        [TestCase(LastNewLine.No, ExpectedResult = "Name,Age\nAlice,27\nBob,26")]
        [TestCase(LastNewLine.Yes, ExpectedResult = "Name,Age\nAlice,27\nBob,26\n")]
        public string Contain_Csv(LastNewLine lastNewLine)
        {
            var persons = new[]
            {
                new Person("Alice", 27),
                new Person("Bob", 26)
            };
            using var csvStream = CsvStream
                .Of(persons, Configuration, lastNewLine)
                .Build();
            using var reader = new StreamReader(csvStream, Encoding.UTF8);

            var csv = reader.ReadToEnd();

            return csv;
        }

        [Test]
        [TestCase(LastNewLine.No, ExpectedResult = "Name,Age\nCharlie,9\nDaisy,7")]
        [TestCase(LastNewLine.Yes, ExpectedResult = "Name,Age\nCharlie,9\nDaisy,7\n")]
        public async Task<string> Contain_Csv_Async(LastNewLine lastNewLine)
        {
            var persons = new[]
            {
                new Person("Charlie", 9),
                new Person("Daisy", 7)
            };
#if !NET472
            await
#endif
            using var csvStream = CsvStream
                .Of(persons.ToAsyncEnumerable(), Configuration, lastNewLine)
                .Build();
            using var reader = new StreamReader(csvStream, Encoding.UTF8);

            var csv = await reader.ReadToEndAsync();

            return csv;
        }

        [Test]
        public void Contain_Anonymous_Type_Rows()
        {
            var drinks = new[]
            {
                new
                {
                    Name = "Green tea",
                    Feature = "Improve brain function",
                    Origin = "China"
                },
                new
                {
                    Name = "Coffee",
                    Feature = "Invigorate",
                    Origin = "Africa"
                },
                new
                {
                    Name = "Kvass",
                    Feature = "Good for okroshka",
                    Origin = "Russia"
                }
            };
            using var csvStream = CsvStream.Of(
                    drinks,
                    Configuration
                )
                .Build();
            using var reader = new StreamReader(csvStream, Encoding.UTF8);

            var csv = reader.ReadToEnd();

            csv.Should().Be(
                "Name,Feature,Origin\n" +
                "Green tea,Improve brain function,China\n" +
                "Coffee,Invigorate,Africa\n" +
                "Kvass,Good for okroshka,Russia"
            );
        }

        private static CsvConfiguration Configuration => new(CultureInfo.InvariantCulture)
        {
            NewLine = NewLine.LF,
            Encoding = Encoding.UTF8
        };

        public class Person
        {
            public Person(string name, int age)
            {
                Name = name;
                Age = age;
            }

            public string Name { get; }
            public int Age { get; }
        }
    }
}