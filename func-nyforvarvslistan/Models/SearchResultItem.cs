using func_nyforvarvslistan;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class ElasticSearchResponse
{
    // ... other properties ...

    public Hits Hits { get; set; }
}

public class Hits
{
    // ... other properties ...

    public List<Hit> hits { get; set; }
}

public class Hit
{
    public Source _source { get; set; }
}

public class Source
{
    public List<String> Classification { get; set; }
    public List<String> PublicationCategories { get; set; }

    public SearchResultItem SearchResultItem { get; set; }

    [JsonProperty("Editors")]
    public List<Editor> Editors { get; set; }

    [JsonProperty("Translators")]
    public List<Translator> Translator { get; set; }
    [JsonProperty("TargetAudience")]
    public string AgeGroup { get; set; }
}



public class Publisher
{
    public string Name { get; set; }
}

public class SearchResultItem
{
   // [JsonConverter(typeof(SingleOrArrayConverter<string>))]
    public List<Author> Author { get; set; }
    [JsonProperty("x-mtm-narrators")]
    public List<Narrator> Narrator { get; set; }
    public List<Translator> Translator { get; set; }
    public List<Publisher> Publisher { get; set; }

    public string Identifier { get; set; }

    [JsonProperty("x-mtm-language")]
    public string Language { get; set; }

    public DateTime Modified { get; set; }

    public string Title { get; set; }

    [JsonProperty("@type")]
    public string Type { get; set; }

    [JsonProperty("x-cover-href")]
    public string CoverHref { get; set; }

    [JsonProperty("x-has-sample")]
    public bool HasSample { get; set; }

    [JsonProperty("x-is-ebook")]
    public bool IsEbook { get; set; }

    [JsonProperty("x-is-audio-book")]
    public bool IsAudioBook { get; set; }

    [JsonProperty("x-has-text")]
    public bool HasText { get; set; }

    [JsonProperty("x-mtm-file-size")]
    public long FileSize { get; set; }

    [JsonProperty("x-library-id")]
    public string LibraryId { get; set; }
    [JsonProperty("x-mtm-libris-id")]
    public string LibrisId { get; set; }
    [JsonProperty("x-mtm-description")]
    public string Description { get; set; }
    [JsonProperty("x-target-audience")]
    public string TargetAudience { get; set; }
    [JsonProperty("x-mtm-format")]
    public string Format { get; set; }
    [JsonProperty("x-mtm-volume")]
    public string Volume { get; set; }
    [JsonProperty("x-mtm-remark")]
    public string Remark { get; set; }
    [JsonProperty("x-mtm-is-under-production")]
    public bool UnderProduction { get; set; }
    [JsonProperty("x-mtm-extent")]
    public List<String> Extent { get; set; }
}

public class Editor
{
    [JsonProperty("name")]
    public string Name
    {
        get; set;
    }
}

public class Item
{
    [JsonProperty("translator")]
    public List<Translator> Translator { get; set; }
}

public class Author
{
    [JsonProperty("x-mtm-primary-contributor")]
    public bool IsPrimaryContributor { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}
public class Narrator
{
    [JsonProperty("name")]
    public string Name { get; set; }
}
public class Translator
{
    [JsonProperty("name")]
    public string Name { get; set; }
}