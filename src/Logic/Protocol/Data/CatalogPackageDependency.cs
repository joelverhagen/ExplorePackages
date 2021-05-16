﻿using Newtonsoft.Json;

namespace NuGet.Insights
{
    public class CatalogPackageDependency
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("range")]
        [JsonConverter(typeof(CatalogPackageDependencyRangeConverter))]
        public string Range { get; set; }
    }
}
