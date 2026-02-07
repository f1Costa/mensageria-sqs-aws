variable "aws_region" {
  type        = string
  description = "Região AWS"
  default     = "us-east-1"
}

variable "project_name" {
  type        = string
  description = "mensageria"
  default     = "queueflow"
}

variable "env" {
  type        = string
  description = "Ambiente (dev/stage/prod)"
  default     = "dev"
}

variable "instance_type" {
  type        = string
  description = "Tipo de instância EC2 para o ECS (EC2 launch type)"
  default     = "t3.micro"
}

variable "ecs_desired_capacity" {
  type        = number
  description = "Quantidade desejada de instâncias no cluster ECS"
  default     = 3
}

variable "worker_image_tag" {
  type        = string
  description = "Tag da imagem do Worker no ECR"
  default     = "latest"
}

variable "lambda_package_path" {
  type        = string
  description = "Caminho para o .zip do Lambda (gerado pelo dotnet publish/zip)"
}

variable "tags" {
  type        = map(string)
  description = "Tags adicionais"
  default     = {}
}
