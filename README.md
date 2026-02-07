<div align="center">

# QueueFlow

**Cloud-native message queue application built on AWS with serverless REST API and ECS-based async workers.**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![AWS](https://img.shields.io/badge/AWS-Cloud-FF9900?style=for-the-badge&logo=amazonwebservices&logoColor=white)](https://aws.amazon.com/)
[![Terraform](https://img.shields.io/badge/Terraform-%3E%3D1.6-7B42BC?style=for-the-badge&logo=terraform&logoColor=white)](https://www.terraform.io/)
[![Docker](https://img.shields.io/badge/Docker-Container-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-Proprietary-red?style=for-the-badge)](#license)

<br/>

<img src="https://img.shields.io/badge/Lambda-Serverless-FF9900?logo=awslambda&logoColor=white" alt="Lambda" />
<img src="https://img.shields.io/badge/API_Gateway-HTTP-FF4F8B?logo=amazonapigateway&logoColor=white" alt="API Gateway" />
<img src="https://img.shields.io/badge/SNS-Pub%2FSub-FF4F8B?logo=amazonsqs&logoColor=white" alt="SNS" />
<img src="https://img.shields.io/badge/SQS-Queue-FF4F8B?logo=amazonsqs&logoColor=white" alt="SQS" />
<img src="https://img.shields.io/badge/ECS-Container-FF9900?logo=amazonecs&logoColor=white" alt="ECS" />
<img src="https://img.shields.io/badge/CloudWatch-Logs-FF4F8B?logo=amazoncloudwatch&logoColor=white" alt="CloudWatch" />

---

[Architecture](#architecture) &bull; [Tech Stack](#tech-stack) &bull; [Getting Started](#getting-started) &bull; [API Reference](#api-endpoints) &bull; [Configuration](#configuration) &bull; [Cleanup](#cleanup)

</div>

<br/>

## Architecture

```
                         ┌──────────────────┐
                         │  HTTP API Gateway │
                         └────────┬─────────┘
                                  │
              ┌───────────────────┼───────────────────┐
              │                   │                   │
        POST /publish       POST /enqueue        GET /status
              │                   │                   │
              └─────────┬─────────┘                   │
                        ▼                             │
               ┌─────────────────┐                    │
               │  Lambda (.NET)  │◄───────────────────┘
               └───┬─────────┬───┘
                   │         │
                   ▼         ▼
             ┌──────────┐ ┌─────────┐
             │ SNS Topic│ │SQS Queue│
             └────┬─────┘ └────┬────┘
                  │            │
                  └──────┬─────┘  (SNS → SQS subscription)
                         ▼
                   ┌──────────┐
                   │SQS Queue │
                   └────┬─────┘
                        ▼
              ┌───────────────────┐
              │  ECS Worker (.NET)│
              │  Long-polling     │
              │  Batch processing │
              └───────────────────┘
```

## Tech Stack

| Layer | Technology |
|:------|:-----------|
| **Language** | C# 12 / .NET 8.0 |
| **API** | AWS Lambda + API Gateway v2 (HTTP) |
| **Messaging** | AWS SNS + SQS |
| **Worker** | AWS ECS on EC2 (Docker) |
| **Registry** | AWS ECR |
| **Networking** | AWS VPC (2 public subnets) |
| **Observability** | AWS CloudWatch Logs |
| **IaC** | Terraform >= 1.6.0 |

## Project Structure

```
queueflow/
├── src/
│   ├── MessagingApiLambda/           # Serverless REST API
│   │   ├── Function.cs               # Lambda handler (publish, enqueue, status)
│   │   └── MessagingApiLambda.csproj
│   └── SqsWorker/                    # Queue consumer service
│       ├── Program.cs                # Long-polling worker loop
│       ├── Dockerfile                # Multi-stage build (.NET 8)
│       └── SqsWorker.csproj
├── terraform/
│   ├── main.tf                       # Root module composition
│   ├── variables.tf                  # Input variables
│   ├── outputs.tf                    # Exported values
│   ├── terraform.tfvars.example      # Configuration template
│   └── modules/
│       ├── vpc/                      # VPC, subnets, internet gateway
│       ├── ecr/                      # Container registry
│       ├── messaging/                # SNS topic, SQS queue, subscription
│       ├── api/                      # Lambda function, API Gateway, IAM
│       ├── ecs_cluster/              # ECS cluster, ASG, capacity provider
│       └── ecs_worker_service/       # Task definition, service, IAM
└── queueflow.sln
```

## Prerequisites

| Tool | Version | Link |
|:-----|:--------|:-----|
| AWS CLI | v2+ | [Install](https://aws.amazon.com/cli/) |
| Terraform | >= 1.6.0 | [Install](https://www.terraform.io/) |
| .NET SDK | 8.0 | [Install](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Docker | Latest | [Install](https://www.docker.com/) |

## Getting Started

### 1. Build the Lambda package

```bash
cd src/MessagingApiLambda
dotnet restore
dotnet publish -c Release -r linux-x64 --self-contained false -o out
cd out && zip -r ../MessagingApiLambda.zip . && cd ../../..
```

### 2. Provision infrastructure

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars to match your environment
terraform init
terraform plan
terraform apply
```

### 3. Build and push the worker image

```bash
ECR_URL=$(cd terraform && terraform output -raw ecr_repository_url)
REGION=$(cd terraform && terraform output -raw aws_region 2>/dev/null || echo "us-east-1")

aws ecr get-login-password --region "$REGION" | \
  docker login --username AWS --password-stdin "$ECR_URL"

docker build -t worker:latest ./src/SqsWorker
docker tag worker:latest "$ECR_URL:latest"
docker push "$ECR_URL:latest"
```

### 4. Verify

```bash
API=$(cd terraform && terraform output -raw api_endpoint)

# Health check
curl -s "$API/status" | jq

# Publish via SNS -> SQS
curl -s -X POST "$API/publish" \
  -H "Content-Type: application/json" \
  -d '{"message": "hello from sns"}' | jq

# Enqueue directly to SQS
curl -s -X POST "$API/enqueue" \
  -H "Content-Type: application/json" \
  -d '{"message": "hello from sqs"}' | jq
```

Check worker logs:

```bash
aws logs tail /ecs/queueflow-dev-worker --follow
```

## API Endpoints

### `GET /status`

Health check endpoint.

**Response:**

```json
{
  "ok": true,
  "service": "queueflow-messaging-api",
  "time": "2025-01-01T00:00:00Z"
}
```

### `POST /publish`

Publishes a message to the SNS topic, which fans out to the SQS queue via subscription.

**Request body:**

```json
{
  "message": "order.created",
  "attributes": { "orderId": "12345" }
}
```

### `POST /enqueue`

Sends a message directly to the SQS queue.

**Request body:**

```json
{
  "message": "process.this",
  "attributes": { "priority": "high" }
}
```

**Response (both endpoints):**

```json
{ "ok": true, "messageId": "..." }
```

## Configuration

| Variable | Description | Default |
|:---------|:-----------|:--------|
| `project_name` | Prefix for all AWS resources | `queueflow` |
| `environment` | Deployment environment | `dev` |
| `aws_region` | AWS region | `us-east-1` |
| `instance_type` | EC2 instance type for ECS nodes | `t3.micro` |
| `desired_capacity` | Number of ECS instances | `3` |
| `lambda_zip_path` | Path to the Lambda deployment zip | `../src/...` |

> See [`terraform/terraform.tfvars.example`](terraform/terraform.tfvars.example) for a complete template.

## Terraform Outputs

| Output | Description |
|:-------|:-----------|
| `api_endpoint` | HTTP API Gateway base URL |
| `ecr_repository_url` | ECR repository for worker images |
| `sns_topic_arn` | SNS topic ARN |
| `sqs_queue_url` | SQS queue URL |
| `ecs_cluster_name` | ECS cluster name |
| `worker_service_name` | ECS worker service name |
| `worker_log_group` | CloudWatch log group for the worker |

## Cleanup

```bash
cd terraform
terraform destroy
```

## License

This project is proprietary. All rights reserved.

---

<div align="center">

Built with .NET, AWS and Terraform

</div>
