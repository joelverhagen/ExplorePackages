﻿using Microsoft.Azure.WebJobs.Host.Queues;

namespace NuGet.Insights.Worker
{
    public class UnencodedQueueProcessorFactory : IQueueProcessorFactory
    {
        public QueueProcessor Create(QueueProcessorFactoryContext context)
        {
            context.Queue.EncodeMessage = false;
            if (context.PoisonQueue != null)
            {
                context.PoisonQueue.EncodeMessage = false;
            }

            return new QueueProcessor(context);
        }
    }
}
