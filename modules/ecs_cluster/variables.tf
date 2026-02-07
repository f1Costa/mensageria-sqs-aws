variable "name_prefix" { type = string }
variable "vpc_id" { type = string }
variable "public_subnet_ids" { type = list(string) }
variable "instance_type" { type = string }
variable "desired_capacity" { type = number }
variable "tags" { type = map(string) }
