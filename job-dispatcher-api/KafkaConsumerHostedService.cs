using client.cassandra.core;
using client.kafka.consumer.core;
using client.kafka.producer.core;
using client.socket.core;
using Confluent.Kafka;
using job_dispatcher.src.main.core.dispatcher;
using job_dispatcher.src.main.core.dispatcher.provider;
using job_dispatcher.src.main.core.job;
using job_dispatcher.src.main.core.worker;
using job_dispatcher_api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace job_dispatcher_api
{
    public class KafkaConsumerHostedService : IHostedService
    {
        private readonly ILogger<KafkaConsumerHostedService> _logger;
        private readonly IKafkaConsumerProvider _kafkaConsumerProvider;
        private readonly IConfiguration _configuration;
        private readonly SampleSocketMessageHandler _sampleSocketMessageHandler;
        private readonly ICassandraClientProvider _cassandraClientProvider;
        private readonly IKafkaProducerProvider _kafkaProducerProvider;
        public KafkaConsumerHostedService(IKafkaConsumerProvider kafkaConsumerProvider, IKafkaProducerProvider kafkaProducerProvider, ICassandraClientProvider cassandraClientProvider, SampleSocketMessageHandler sampleSocketMessageHandler, ILogger<KafkaConsumerHostedService> logger, IConfiguration configuration)
        {
            _kafkaConsumerProvider = kafkaConsumerProvider;
            _logger = logger;
            _configuration = configuration;
            _sampleSocketMessageHandler = sampleSocketMessageHandler;
            _cassandraClientProvider = cassandraClientProvider;
            _kafkaProducerProvider = kafkaProducerProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                IDispatcher dispatcher = DispatcherProvider.GetDispatcher("my-dispatcher-1", 20, 10000);

                dispatcher.Start();

                using (IKafkaConsumer<Ignore, string> c = this._kafkaConsumerProvider.GetKafkaConsumer<Ignore, string>("consumer-selection-count"))
                {
                    c.Subscribe();
                    c.Assign("Test_Records", 0);

                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var cr = c.Consume(cancellationToken);
                                //_logger.LogInformation($"1. Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.");
                                SelectionSummaryKafkaMessage kafkaSocketMessage = null;
                                try
                                {
                                    kafkaSocketMessage = cr.Message?.Value?.FromJSON<SelectionSummaryKafkaMessage>();
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Exception occured in KafkaSocketMessage deserialization: {ex.Message}");
                                }

                                if (kafkaSocketMessage != null)
                                {
                                    SocketMessage calculationResultMessage = new SocketMessage()
                                    {
                                        Code = "selection_count_calculated",
                                        Data = kafkaSocketMessage
                                    };

                                    //Task.Run(async () => { await _sampleSocketMessageHandler.SendMessageAsync(kafkaSocketMessage.UserId, calculationResultMessage.ToJSON()); });

                                    Task.Run(async () =>
                                    {
                                        IJob job = new MyJob(cr.Offset.Value, _cassandraClientProvider);
                                        await dispatcher.AddJob(job);
                                    });
                                }
                            }
                            catch (ConsumeException e)
                            {
                                _logger.LogError($"Error occured: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException e)
                    {
                        _logger.LogError($"OperationCanceledException occured: {e.Message}");
                        c.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception occured: {ex.Message}");
                    }
                }

                dispatcher.Stop();

            }, cancellationToken);


            Task.Run(() =>
            {
                IDispatcher dispatcher = DispatcherProvider.GetDispatcher("my-dispatcher-2", 20, 10000);

                dispatcher.Start();

                using (IKafkaConsumer<Ignore, string> c = this._kafkaConsumerProvider.GetKafkaConsumer<Ignore, string>("consumer-selection-count"))
                {
                    c.Subscribe();
                    c.Assign("Test_Records", 1);

                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var cr = c.Consume(cancellationToken);
                                //_logger.LogInformation($"1. Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.");
                                SelectionSummaryKafkaMessage kafkaSocketMessage = null;
                                try
                                {
                                    kafkaSocketMessage = cr.Message?.Value?.FromJSON<SelectionSummaryKafkaMessage>();
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Exception occured in KafkaSocketMessage deserialization: {ex.Message}");
                                }

                                if (kafkaSocketMessage != null)
                                {
                                    SocketMessage calculationResultMessage = new SocketMessage()
                                    {
                                        Code = "selection_count_calculated",
                                        Data = kafkaSocketMessage
                                    };

                                    //Task.Run(async () => { await _sampleSocketMessageHandler.SendMessageAsync(kafkaSocketMessage.UserId, calculationResultMessage.ToJSON()); });

                                    Task.Run(async () =>
                                    {
                                        IJob job = new MyJob(cr.Offset.Value, _cassandraClientProvider);
                                        await dispatcher.AddJob(job);
                                    });
                                }
                            }
                            catch (ConsumeException e)
                            {
                                _logger.LogError($"Error occured: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException e)
                    {
                        _logger.LogError($"OperationCanceledException occured: {e.Message}");
                        c.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception occured: {ex.Message}");
                    }
                }

                dispatcher.Stop();

            }, cancellationToken);

            Task.Run(async () =>
            {

                DeliveryResult<Null, string> dr = null;
                using (IKafkaProducer<Null, string> p = this._kafkaProducerProvider.GetKafkaProducer<Null, string>("producer-1"))
                {
                    while (true)
                    {
                        try
                        {
                            dr = await p.ProduceAsync(new Message<Null, string> { Value = (new SelectionSummaryKafkaMessage() { Count = 1, Name = "test", UserId = "test-user" }).ToJSON() });
                            //_logger.LogInformation($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
                        }
                        catch (ProduceException<Null, string> e)
                        {
                            _logger.LogError($"Delivery failed: {e.Error.Reason}");
                        }
                    }
                }

            }, cancellationToken);

            Task.Run(async () =>
            {

                DeliveryResult<Null, string> dr = null;
                using (IKafkaProducer<Null, string> p = this._kafkaProducerProvider.GetKafkaProducer<Null, string>("producer-2"))
                {
                    while (true)
                    {
                        try
                        {
                            dr = await p.ProduceAsync(new Message<Null, string> { Value = (new SelectionSummaryKafkaMessage() { Count = 1, Name = "test", UserId = "test-user" }).ToJSON() });
                            //_logger.LogInformation($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
                        }
                        catch (ProduceException<Null, string> e)
                        {
                            _logger.LogError($"Delivery failed: {e.Error.Reason}");
                        }
                    }
                }

            }, cancellationToken);

            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }


    public class MyJob : Job, IJob
    {
        private static ICassandraClient _cassandraCliet;
        public MyJob(object data, ICassandraClientProvider cassandraClientProvider) : base(data)
        {
            if (_cassandraCliet == null)
            {
                _cassandraCliet = cassandraClientProvider.GetCassandraClient("my_cluster", "my_keyspace");
            }
        }
        public override async Task Do(IWorker worker)
        {
            await Task.Run(() =>
            {
                var id = Convert.ToString(data);
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                _cassandraCliet.Execute($"insert into test_records (id, name) VALUES ('{id}', '{id}')");

                watch.Stop();

                Console.WriteLine($"cassandra execution time: {watch.ElapsedMilliseconds} ms");

                //if (worker.GetId() == 0)
                //{
                //    await Task.Delay(30000);
                //}
            });
        }
    }



    //public class MyJob : Job, IJob
    //{
    //    private readonly ICassandraClientProvider _cassandraClientProvider;
    //    public MyJob(object data, ICassandraClientProvider cassandraClientProvider) : base(data)
    //    {
    //        _cassandraClientProvider = cassandraClientProvider;
    //    }
    //    public override async Task Do(IWorker worker)
    //    {
    //        await Task.Run(() =>
    //        {
    //            var id = Convert.ToString(data);
    //            var watch = new System.Diagnostics.Stopwatch();
    //            watch.Start();
    //            using (ICassandraClient client = this._cassandraClientProvider.GetCassandraClient("my_cluster", "my_keyspace"))
    //            {
    //                client.Execute($"insert into test_records (id, name) VALUES ('{id}', '{id}')");
    //            }

    //            watch.Stop();

    //            Console.WriteLine($"cassandra execution time: {watch.ElapsedMilliseconds} ms");

    //            //if (worker.GetId() == 0)
    //            //{
    //            //    await Task.Delay(30000);
    //            //}
    //        });
    //    }
    //}


    public static class JsonSerialization
    {
        public static string ToJSON<T>(this T data)
        {
            return JsonSerializer.Serialize<T>(data);
        }

        public static T FromJSON<T>(this string data)
        {
            return JsonSerializer.Deserialize<T>(data);
        }
    }

    public static class HostedServiceExtensions
    {
        public static IServiceCollection AddHostedServices(this IServiceCollection services, List<Assembly> workersAssemblies)
        {
            MethodInfo methodInfo =
                typeof(ServiceCollectionHostedServiceExtensions)
                .GetMethods()
                .FirstOrDefault(p => p.Name == nameof(ServiceCollectionHostedServiceExtensions.AddHostedService));

            if (methodInfo == null)
                throw new Exception($"Impossible to find the extension method '{nameof(ServiceCollectionHostedServiceExtensions.AddHostedService)}' of '{nameof(IServiceCollection)}'.");

            IEnumerable<Type> hostedServices_FromAssemblies = workersAssemblies.SelectMany(a => a.DefinedTypes).Where(x => x.GetInterfaces().Contains(typeof(IHostedService))).Select(p => p.AsType());

            foreach (Type hostedService in hostedServices_FromAssemblies)
            {
                if (typeof(IHostedService).IsAssignableFrom(hostedService))
                {
                    var genericMethod_AddHostedService = methodInfo.MakeGenericMethod(hostedService);
                    _ = genericMethod_AddHostedService.Invoke(obj: null, parameters: new object[] { services }); // this is like calling services.AddHostedService<T>(), but with dynamic T (= backgroundService).
                }
            }

            return services;
        }
    }

}
