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
docker tag nginx:latest my-local-registry.uksouth.azurecontainer.io/test/nginx:latest
docker images
```