variable "name_prefix" { type = string }
variable "tags" { type = map(string) }

variable "cluster_name" { type = string }
variable "capacity_provider" { type = string }

variable "vpc_id" { type = string }
variable "subnet_ids" { type = list(string) }

variable "sqs_queue_url" { type = string }
variable "sqs_queue_arn" { type = string }

variable "ecr_image" {
  type        = string
  description = "Imagem completa (repo_url:tag)"
}

variable "desired_count" {
  type    = number
  default = 1
}

variable "cpu" {
  type    = number
  default = 256
}

variable "memory" {
  type    = number
  default = 512
}
