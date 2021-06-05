﻿using System.Collections.Generic;
using Knapcode.ExplorePackages.Worker;

namespace Knapcode.ExplorePackages.Website
{
    public class ExplorePackagesWebsiteSettings : ExplorePackagesWorkerSettings
    {
        public bool ShowAdminLink { get; set; } = false;
        public bool RestrictUsers { get; set; } = true;
        public List<AllowedObject> AllowedUsers { get; set; } = new List<AllowedObject>();
        public List<AllowedObject> AllowedGroups { get; set; } = new List<AllowedObject>();
    }
}
