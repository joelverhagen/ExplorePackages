﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Azure.Data.Tables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NuGet.Insights.Worker
{
    public class TableScanMessage<T> where T : class, ITableEntity, new()
    {
        [JsonProperty("b")]
        public DateTimeOffset Started { get; set; }

        [JsonProperty("ts")]
        public TaskStateKey TaskStateKey { get; set; }

        [JsonProperty("t")]
        public TableScanDriverType DriverType { get; set; }

        [JsonProperty("n")]
        public string TableName { get; set; }

        [JsonProperty("s")]
        public TableScanStrategy Strategy { get; set; }

        [JsonProperty("c")]
        public int TakeCount { get; set; }

        [JsonProperty("p")]
        public string PartitionKeyPrefix { get; set; }

        [JsonProperty("e")]
        public bool ExpandPartitionKeys { get; set; }

        [JsonProperty("v")]
        public JToken ScanParameters { get; set; }

        [JsonProperty("d")]
        public JToken DriverParameters { get; set; }
    }
}
