# PolymorphicJsonConverter
## samples
```csharp
using Newtonsoft.Json;

namespace Sample.Serialization.Library
{
    [JsonConverter(typeof(PolymorphicJsonConverter<Person>))]
    record Person(string Name, int Age);
    record PersonWithAddress(string Name, int Age, string Address) : Person(Name, Age);

    record PersonWithPhone(string Name, int Age, string Phone) : Person(Name, Age);
    record PersonWithAddressAndPhone(string Name, int Age, string Address, string Phone) : Person(Name, Age);

    class MyClasstests
    {
        [Fact]
        public void test()
        {
            var person = new Person("John Doe", 30);
            var personWithAddress = new PersonWithAddress("Jane Doe", 28, "123 Main St");
            var personWithPhone = new PersonWithPhone("Alice Smith", 25, "555-1234");
            var personWithAddressAndPhone = new PersonWithAddressAndPhone("Bob Johnson", 35, "456 Elm St", "555-5678");
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };
            string jsonPerson = JsonConvert.SerializeObject(person, settings);
            string jsonPersonWithAddress = JsonConvert.SerializeObject(personWithAddress, settings);
            string jsonPersonWithPhone = JsonConvert.SerializeObject(personWithPhone, settings);
            string jsonPersonWithAddressAndPhone = JsonConvert.SerializeObject(personWithAddressAndPhone, settings);
            System.Console.WriteLine(jsonPerson);
            System.Console.WriteLine(jsonPersonWithAddress);
            System.Console.WriteLine(jsonPersonWithPhone);
            System.Console.WriteLine(jsonPersonWithAddressAndPhone);
            var deserializedPerson = JsonConvert.DeserializeObject<Person>(jsonPerson, settings);
            var deserializedPersonWithAddress = JsonConvert.DeserializeObject<PersonWithAddress>(jsonPersonWithAddress, settings);
            var deserializedPersonWithPhone = JsonConvert.DeserializeObject<PersonWithPhone>(jsonPersonWithPhone, settings);
            var deserializedPersonWithAddressAndPhone = JsonConvert.DeserializeObject<PersonWithAddressAndPhone>(jsonPersonWithAddressAndPhone, settings);
            System.Console.WriteLine($"Deserialized Person: {deserializedPerson.Name}, Age: {deserializedPerson.Age}");
        }
    }
}

```