﻿using System;

namespace Knapcode.ExplorePackages.WideEntities
{
    public class WideEntityOperation
    {
        public WideEntityOperation(string partitionKey)
        {
            PartitionKey = partitionKey;
        }

        public string PartitionKey { get; }

        public static WideEntityReplaceOperation Replace(WideEntity existing, ReadOnlyMemory<byte> content)
        {
            return new WideEntityReplaceOperation(existing, content);
        }

        public static WideEntityInsertOperation Insert(string partitionKey, string rowKey, ReadOnlyMemory<byte> content)
        {
            return new WideEntityInsertOperation(partitionKey, rowKey, content);
        }

        public static WideEntityInsertOrReplaceOperation InsertOrReplace(string partitionKey, string rowKey, ReadOnlyMemory<byte> content)
        {
            return new WideEntityInsertOrReplaceOperation(partitionKey, rowKey, content);
        }

        public static WideEntityDeleteOperation Delete(WideEntity existing)
        {
            return new WideEntityDeleteOperation(existing);
        }
    }
}