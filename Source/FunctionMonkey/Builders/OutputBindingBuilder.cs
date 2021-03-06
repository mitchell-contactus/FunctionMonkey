using System.Collections;
using System.IO;
using FunctionMonkey.Abstractions.Builders;
using FunctionMonkey.Abstractions.Builders.Model;
using FunctionMonkey.Abstractions.SignalR;
using FunctionMonkey.Model.OutputBindings;

namespace FunctionMonkey.Builders
{
    internal class OutputBindingBuilder<TParentBuilder> : IOutputBindingBuilder<TParentBuilder>
    {
        private readonly ConnectionStringSettingNames _connectionStringSettingNames;
        private readonly TParentBuilder _parentBuilder;
        private readonly AbstractFunctionDefinition _functionDefinition;

        public OutputBindingBuilder(ConnectionStringSettingNames connectionStringSettingNames, TParentBuilder parentBuilder, AbstractFunctionDefinition functionDefinition)
        {
            _connectionStringSettingNames = connectionStringSettingNames;
            _parentBuilder = parentBuilder;
            _functionDefinition = functionDefinition;
        }
        
        public TParentBuilder ServiceBusQueue(string connectionString, string queueName)
        {
            VerifyOutputBinding();
            _functionDefinition.OutputBinding = new ServiceBusQueueOutputBinding(_functionDefinition.CommandResultItemTypeName, connectionString)
            {
                QueueName = queueName
            };

            return _parentBuilder;
        }

        public TParentBuilder ServiceBusQueue(string queueName)
        {
            return ServiceBusQueue(_connectionStringSettingNames.ServiceBus, queueName);
        }

        public TParentBuilder ServiceBusTopic(string connectionString, string topicName)
        {
            VerifyOutputBinding();
            _functionDefinition.OutputBinding = new ServiceBusTopicOutputBinding(_functionDefinition.CommandResultItemTypeName, connectionString)
            {
                TopicName = topicName
            };

            return _parentBuilder;
        }

        public TParentBuilder ServiceBusTopic(string topicName)
        {
            return ServiceBusTopic(_connectionStringSettingNames.ServiceBus, topicName);
        }

        public TParentBuilder SignalRMessage(string hubName)
        {
            return SignalRMessage(_connectionStringSettingNames.SignalR, hubName);
        }

        public TParentBuilder SignalRMessage(string connectionStringSettingName, string hubName)
        {
            VerifyOutputBinding();
            if (!typeof(SignalRMessage).IsAssignableFrom(_functionDefinition.CommandResultItemType))
            {
                throw new ConfigurationException("Commands that use SignalRMessage output bindings must return a FunctionMonkey.Abstractions.SignalR.SignalRMessage class or a derivative");
            }
            _functionDefinition.OutputBinding = new SignalROutputBinding(_functionDefinition.CommandResultItemTypeName, connectionStringSettingName)
            {
                HubName = hubName,
                SignalROutputTypeName = "Microsoft.Azure.WebJobs.Extensions.SignalRService.SignalRMessage" // can't use typeof() here as we don't want to bring the SignalR package into here
            };
            return _parentBuilder;
        }

        public TParentBuilder SignalRGroupAction(string connectionStringSettingName, string hubName)
        {
            VerifyOutputBinding();
            if (!typeof(SignalRGroupAction).IsAssignableFrom(_functionDefinition.CommandResultItemType))
            {
                throw new ConfigurationException("Commands that use SignalRGroupAction output bindings must return a FunctionMonkey.Abstractions.SignalR.SignalRGroupAction class or a derivative");
            }

            _functionDefinition.OutputBinding = new SignalROutputBinding(_functionDefinition.CommandResultItemTypeName,
                connectionStringSettingName)
            {
                HubName = hubName,
                SignalROutputTypeName = "Microsoft.Azure.WebJobs.Extensions.SignalRService.SignalRGroupAction" // can't use typeof() here as we don't want to bring the SignalR package into here
            };
            return _parentBuilder;
        }

        public TParentBuilder SignalRGroupAction(string hubName)
        {
            return SignalRGroupAction(_connectionStringSettingNames.SignalR, hubName);
        }

        public TParentBuilder StorageBlob(string connectionStringSettingName, string name, FileAccess fileAccess = FileAccess.Write)
        {
            if (_functionDefinition.OutputBinding is null)
            {
                _functionDefinition.OutputBinding = new StorageBlobOutputBinding(_functionDefinition.CommandResultItemTypeName);
            }

            if (_functionDefinition.OutputBinding is StorageBlobOutputBinding blobBinding)
            {
                blobBinding.Outputs.Add(new StorageBlobOutput(_functionDefinition.CommandResultItemTypeName, connectionStringSettingName)
                {
                    FileAccess = fileAccess,
                    Name = name
                });
            }
            else
            {
                throw new ConfigurationException($"An output binding is already set for command {_functionDefinition.CommandType.Name}");
            }

            return _parentBuilder;
        }

        public TParentBuilder StorageBlob(string name, FileAccess fileAccess = FileAccess.Write)
        {
            return StorageBlob(_connectionStringSettingNames.Storage, name, fileAccess);
        }

        public TParentBuilder StorageQueue(string connectionStringSettingName, string queueName)
        {
            VerifyOutputBinding();
            _functionDefinition.OutputBinding = new StorageQueueOutputBinding(_functionDefinition.CommandResultItemTypeName, connectionStringSettingName)
            {
                QueueName = queueName
            };
            return _parentBuilder;
        }

        public TParentBuilder StorageQueue(string queueName)
        {
            return StorageQueue(_connectionStringSettingNames.Storage, queueName);
        }

        public TParentBuilder StorageTable(string connectionStringSettingName, string tableName)
        {
            VerifyOutputBinding();
            _functionDefinition.OutputBinding = new StorageTableOutputBinding(_functionDefinition.CommandResultItemTypeName, connectionStringSettingName)
            {
                TableName = tableName
            };
            return _parentBuilder;
        }

        public TParentBuilder StorageTable(string tableName)
        {
            return StorageTable(_connectionStringSettingNames.Storage, tableName);
        }

        public TParentBuilder CosmosDb(string connectionStringSettingName, string collectionName, string databaseName)
        {
            VerifyOutputBinding();

            // we can use the command output type to determine whether or not to use a IAsyncCollector or an out parameter
            // if its based on IEnumerable we do the former, otherwise the latter
            bool isCollection = typeof(IEnumerable).IsAssignableFrom(_functionDefinition.CommandResultType);
            
            _functionDefinition.OutputBinding = new CosmosOutputBinding(_functionDefinition.CommandResultItemTypeName, connectionStringSettingName)
            {
                CollectionName = collectionName,
                DatabaseName = databaseName,
                IsCollection = isCollection
            };

            return _parentBuilder;
        }

        public TParentBuilder CosmosDb(string collectionName, string databaseName)
        {
            return CosmosDb(_connectionStringSettingNames.CosmosDb, collectionName, databaseName);
        }

        private void VerifyOutputBinding()
        {
            if (_functionDefinition.OutputBinding != null)
            {
                throw new ConfigurationException($"An output binding is already set for command {_functionDefinition.CommandType.Name}");
            }

            if (!_functionDefinition.CommandHasResult)
            {
                throw new ConfigurationException($"Command of type {_functionDefinition.CommandType.Name} requires a result to be used with an output binding");
            }
        }
    }
}