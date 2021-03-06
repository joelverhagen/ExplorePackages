﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace NuGet.Insights.Worker.BuildVersionSet
{
    public interface IVersionSet
    {
        DateTimeOffset CommitTimestamp { get; }
        IReadOnlyCollection<string> GetUncheckedIds();
        IReadOnlyCollection<string> GetUncheckedVersions(string id);
        bool DidIdEverExist(string id);
        bool DidVersionEverExist(string id, string version);
    }
}
