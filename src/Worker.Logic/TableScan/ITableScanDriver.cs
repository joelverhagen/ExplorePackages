﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Insights.Worker
{
    public interface ITableScanDriver<T>
    {
        IList<string> SelectColumns { get; }
        Task InitializeAsync(JToken parameters);
        Task ProcessEntitySegmentAsync(string tableName, JToken parameters, IReadOnlyList<T> entities);
    }
}
