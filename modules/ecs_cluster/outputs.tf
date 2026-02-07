output "cluster_name" {
  value = aws_ecs_cluster.this.name
}

output "capacity_provider_name" {
  value = aws_ecs_capacity_provider.this.name
}

output "ecs_instance_sg_id" {
  value = aws_security_group.ecs_instances.id
}
