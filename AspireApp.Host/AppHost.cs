var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.KafkaProducer_WebApp>("kafkaproducer-webapp");

builder.AddProject<Projects.KafkaConsumerApp>("kafkaconsumerapp");

builder.Build().Run();
