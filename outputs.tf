output "api_endpoint" {
  value = module.api.api_endpoint
}

output "ecr_repository_url" {
  value = module.ecr.repository_url
}

output "sns_topic_arn" {
  value = module.messaging.sns_topic_arn
}

output "sqs_queue_url" {
  value = module.messaging.sqs_queue_url
}

output "ecs_cluster_name" {
  value = module.ecs_cluster.cluster_name
}

output "worker_service_name" {
  value = module.ecs_worker_service.service_name
}

output "worker_log_group" {
  value = module.ecs_worker_service.log_group_name
}
