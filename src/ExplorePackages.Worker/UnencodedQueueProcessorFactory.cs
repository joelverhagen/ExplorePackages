﻿using Knapcode.ExplorePackages.Worker;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Queues;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Knapcode.ExplorePackages.Worker
{
    public class UnencodedQueueProcessorFactory : IQueueProcessorFactory
    {
        public QueueProcessor Create(QueueProcessorFactoryContext context)
        {
            context.Queue.EncodeMessage = false;
            context.PoisonQueue.EncodeMessage = false;

            return new QueueProcessor(context);
        }
    }
}