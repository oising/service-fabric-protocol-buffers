﻿using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using SceneSkope.HashingFunctions;
using System;

namespace SceneSkope.ServiceFabric.GrpcRemoting
{
    public class GrpcServiceProxyFactory<TClient> where TClient : ClientBase<TClient>
    {
        public GrpcCommunicationClientFactory<TClient> CommunicationClientFactory { get; }
        public Uri ServiceUri { get; }

        public ILogger Log { get; }

        public GrpcServiceProxyFactory(ILogger logger, Func<Channel, TClient> creator, IServicePartitionResolver servicePartitionResolver = null, Uri serviceUri = null, string traceId = null) :
            this(logger, ChannelCache.CreateDefault(logger), creator, servicePartitionResolver, serviceUri, traceId)
        { }

        public GrpcServiceProxyFactory(ILogger logger, ChannelCache channelCache, Func<Channel, TClient> creator, IServicePartitionResolver servicePartitionResolver = null, Uri serviceUri = null, string traceId = null)
        {
            Log = logger;
            ServiceUri = serviceUri;
            CommunicationClientFactory = new GrpcCommunicationClientFactory<TClient>(logger, channelCache, creator, servicePartitionResolver, null, traceId);
        }

        private static ServicePartitionKey GenerateKey(Guid id) => new ServicePartitionKey(Fnv1A.ComputeAllLong(id));

        public ServicePartitionClient<GrpcCommunicationClient<TClient>> CreateProxy(Guid id,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null, OperationRetrySettings retrySettings = null) =>
            CreateProxy(ServiceUri, GenerateKey(id), targetReplicaSelector, listenerName, retrySettings);

        public ServicePartitionClient<GrpcCommunicationClient<TClient>> CreateProxy(Uri serviceUri, Guid id,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null, OperationRetrySettings retrySettings = null) =>
            CreateProxy(serviceUri, GenerateKey(id), targetReplicaSelector, listenerName, retrySettings);

        public ServicePartitionClient<GrpcCommunicationClient<TClient>> CreateProxy(ServicePartitionKey partitionKey = null,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null, OperationRetrySettings retrySettings = null) =>
            CreateProxy(ServiceUri, partitionKey, targetReplicaSelector, listenerName, retrySettings);

        public ServicePartitionClient<GrpcCommunicationClient<TClient>> CreateProxy(Uri serviceUri = null, ServicePartitionKey partitionKey = null,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null, OperationRetrySettings retrySettings = null)
        {
            var realServiceUri = serviceUri ?? ServiceUri ?? throw new ArgumentNullException("A service URI must be specified");
            Log.LogTrace("Create proxy for {Uri} {@Key}", realServiceUri, partitionKey);
            return new ServicePartitionClient<GrpcCommunicationClient<TClient>>(CommunicationClientFactory, realServiceUri, partitionKey,
                targetReplicaSelector, listenerName, retrySettings);
        }
    }
}
