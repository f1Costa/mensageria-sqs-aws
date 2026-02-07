variable "name_prefix" { type = string }
variable "sns_topic_arn" { type = string }
variable "sqs_queue_url" { type = string }
variable "sqs_queue_arn" { type = string }
variable "lambda_package_path" { type = string }
variable "tags" { type = map(string) }
