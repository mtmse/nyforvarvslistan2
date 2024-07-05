using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace func_nyforvarvslistan
{
    public class Book
    {
        public List<Author> Authors { get; set; }
        public List<Narrator> Narrator { get; set; }
        public List<Translator> Translator { get; set; }
        public List<Publisher> PublisherName { get; set; }
        public List<String> Extent { get; set; }
        public List<String> PublicationCategory { get; set; }
        public string Title { get; set; }
        public string CoverHref { get; set; }
        public string Description { get; set; }
        public string LibraryId { get; set; }
        public string Category { get; set; }
        public string AgeGroup { get; set; }
        public string Language { get; set; }
        public string LibrisId { get; set; }
        public string Format { get; set; }
        public string City { get; set; }
        public string PublishingCompany { get; set; }
        public string PublishedYear { get; set; }
    }

    public class SingleOrArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(List<T>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var token = JToken.Load(reader);
                if (token.Type == JTokenType.Array)
                {
                    return token.ToObject<List<T>>();
                }
                return new List<T> { token.ToObject<T>() };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error at path: {reader.Path}, token: {reader.TokenType}, value: {reader.Value}");
                throw;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var list = value as List<T>;
            if (list != null && list.Count == 1)
            {
                value = list[0];
            }
            serializer.Serialize(writer, value);
        }
    }

}
