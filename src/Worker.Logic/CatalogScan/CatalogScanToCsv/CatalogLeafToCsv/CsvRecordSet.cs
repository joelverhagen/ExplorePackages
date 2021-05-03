﻿using System.Collections.Generic;

namespace Knapcode.ExplorePackages.Worker
{
    public class CsvRecordSet<T> : ICsvRecordSet<T> where T : ICsvRecord
    {
        public CsvRecordSet(string bucketKey, IReadOnlyList<T> records)
        {
            Records = records;
            BucketKey = bucketKey;
        }

        /// <summary>
        /// This bucket key will be hashed and used to select a large CSV blob to append results to.
        /// Typically this is a concatenation of the normalized, lowercase package ID and version. This key should be
        /// consistent per package ID or package ID + version to allow for proper data pruning with
        /// <see cref="ICsvResultStorage{T}.Prune(List{T})"/>.
        /// </summary>
        /// <returns>The key used for bucketing returned CSV records.</returns>
        public string BucketKey { get; }

        public IReadOnlyList<T> Records { get; }
    }
}