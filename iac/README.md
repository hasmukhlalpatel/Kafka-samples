# Setup kafka and schema registry in Azure Container Apps

## commands to setup kafka
```bash
az login --tenant TENANT_ID
az container create --resource-group aca-test --file kafka-aci.yaml
az container show --resource-group aca-test --name local-kafka --query ipAddress.fqdn --output tsv
```

### Test the kafka
```bash
docker compose -f docker-compose-kafka.yaml up -d kafka-ui
--change path in compose file
docker compose up -d kafka-ui
```

## Setup schema registry
```bash
az login --tenant TENANT_ID

az container create --resource-group aca-test --file registry-aci.yaml

az container show --resource-group aca-test --name local-registry --query ipAddress.fqdn --output tsv

curl http://my-local-registry-test.uksouth.azurecontainer.io/v2/

```

### Test the registry
```bash
curl http://my-local-registry-test.uksouth.azurecontainer.io/v2/

curl -X POST http://my-local-registry-test.uksouth.azurecontainer.io/v2/test-schema/versions \
  -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  -d '{
	"schema": "{\"type\":\"record\",\"name\":\"TestRecord\",\"fields\":[{\"name\":\"field1\",\"type\":\"string\"}]}'
  }'
```

### Test the registry with images
```bash
docker pull nginx:latest
docker tag nginx:latest my-local-registry-test.uksouth.azurecontainer.io/test/nginx:latest
docker images

podman push --tls-verify=false `
  nginx:latest `
  my-local-registry-test.uksouth.azurecontainer.io/test/nginx:latest
```

## Create Azure contianer registry
```bash
az acr create --resource-group aca-test --name myacr080926 --sku Basic --location uksouth

az acr show --resource-group aca-test --name myacr080926 --query loginServer --output tsv

-- docker login to the registry
az acr login --name myacr080926

-- Get token for podman login
podman login myacr080926.azurecr.io `
  --username 00000000-0000-0000-0000-000000000000 `
  --password "<ACCESS_TOKEN>"

podman login myacr080926.azurecr.io `
  --username 00000000-0000-0000-0000-000000000000 `
  --password "<ACCESS_TOKEN>"

-- podman login to the registry
$token = az acr login `
  --name myacr080926 `
  --expose-token `
  --query accessToken `
  --output tsv

podman login myacr080926.azurecr.io `
  --username 00000000-0000-0000-0000-000000000000 `
  --password $token
```

### push image to Azure container registry
```bash
docker pull nginx:alpine3.24-perl
docker tag nginx:alpine3.24-perl myacr080926.azurecr.io/test/nginx:alpine3.24-perl
docker push myacr080926.azurecr.io/test/nginx:alpine3.24-perl
```

## build and push image to Azure container registry

cd to the solution folder and run the following command to build the image
```bash
docker build -t sample.consumer -f .\src\Samples.Kafka.Consumer.Worker\Dockerfile .
docker images
docker tag localhost/sample.consumer myacr080926.azurecr.io/test/sample.consumer
docker push myacr080926.azurecr.io/test/sample.consumer
```

## deploy the image to Azure Container Apps
```bash
az containerapp create --name sample-consumer-app --resource-group aca-test --image myacr080926.azurecr.io/test/sample.consumer --environment <ENVIRONMENT_NAME> --registry-server myacr080926.azurecr.io --registry-username <USERNAME> --registry-password <PASSWORD>
```

## update sample consumer app to use kafka scalar
```powershell
az containerapp update `
  --name "kafka-consumer" `
  --resource-group "aca-test" `
  --min-replicas 1 `
  --max-replicas 3 `
  --scale-rule-name "kafka-lag" `
  --scale-rule-type "kafka" `
  --scale-rule-metadata `
      "bootstrapServers=my-local-kafka-test.uksouth.azurecontainer.io:9092" `
      "consumerGroup=my-consumer-group" `
      "topic=my-topic" `
      "lagThreshold=10"
```
