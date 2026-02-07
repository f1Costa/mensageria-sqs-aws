module "vpc" {
  source      = "./modules/vpc"
  name_prefix = local.name_prefix
  tags        = local.tags
}

module "ecr" {
  source      = "./modules/ecr"
  name_prefix = local.name_prefix
  tags        = local.tags
}

module "messaging" {
  source      = "./modules/messaging"
  name_prefix = local.name_prefix
  tags        = local.tags
}

module "ecs_cluster" {
  source            = "./modules/ecs_cluster"
  name_prefix       = local.name_prefix
  vpc_id            = module.vpc.vpc_id
  public_subnet_ids = module.vpc.public_subnet_ids
  instance_type     = var.instance_type
  desired_capacity  = var.ecs_desired_capacity
  tags              = local.tags
}

module "api" {
  source              = "./modules/api"
  name_prefix         = local.name_prefix
  sns_topic_arn       = module.messaging.sns_topic_arn
  sqs_queue_url       = module.messaging.sqs_queue_url
  sqs_queue_arn       = module.messaging.sqs_queue_arn
  tags                = local.tags
  lambda_package_path = var.lambda_package_path
}

module "ecs_worker_service" {
  source = "./modules/ecs_worker_service"

  name_prefix       = local.name_prefix
  cluster_name      = module.ecs_cluster.cluster_name
  capacity_provider = module.ecs_cluster.capacity_provider_name

  vpc_id     = module.vpc.vpc_id
  subnet_ids = module.vpc.public_subnet_ids

  sqs_queue_url = module.messaging.sqs_queue_url
  sqs_queue_arn = module.messaging.sqs_queue_arn

  ecr_image     = "${module.ecr.repository_url}:${var.worker_image_tag}"
  desired_count = 1
  cpu           = 256
  memory        = 512

  tags = local.tags
}
