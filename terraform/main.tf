# terraform/main.tf
provider "aws" {
  region = "sa-east-1"
}

# Cluster ECS (Onde vão rodar as APIs e o Motor)
resource "aws_ecs_cluster" "itau_poc_cluster" {
  name = "compra-programada-cluster"
}

# Task Definition da API
resource "aws_ecs_task_definition" "api_task" {
  family                   = "api-compra-programada"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = 512
  memory                   = 1024

  container_definitions = jsonencode([{
    name      = "api-container"
    image     = "seu-repo/compra-programada-api:latest"
    essential = true
    portMappings = [{
      containerPort = 80
      hostPort      = 80
    }]
    environment = [
      { name = "ConnectionStrings__DefaultConnection", value = "Server=meu-rds.aws.com;Database=ItauPoc..." }
    ]
  }])
}

# EventBridge para rodar o Motor (Worker)
# Dispara às 02h00 no dia útil mais próximo dos dias 5, 15 e 25
resource "aws_cloudwatch_event_rule" "motor_cron" {
  name                = "disparo-motor-compras"
  description         = "Roda o motor de consolidação apenas em dias úteis"
  schedule_expression = "cron(0 2 5W,15W,25W * ? *)" 
}

# Associa o CRON à execução de uma Task Fargate do Motor
resource "aws_cloudwatch_event_target" "run_motor_task" {
  target_id = "ExecutarMotor"
  rule      = aws_cloudwatch_event_rule.motor_cron.name
  arn       = aws_ecs_cluster.itau_poc_cluster.arn
  ecs_target {
    task_definition_arn = aws_ecs_task_definition.api_task.arn
    launch_type         = "FARGATE"
    network_configuration {
      subnets = ["subnet-xyz"]
    }
  }
}

# 4. Banco de Dados RDS (MySQL)
resource "aws_db_instance" "database" {
  identifier          = "itau-poc-mysql"
  allocated_storage   = 20
  engine              = "mysql"
  engine_version      = "8.0"
  instance_class      = "db.t3.micro"
  username            = "admin"
  password            = "senha_super_segura"
  port                = 3306
  skip_final_snapshot = true
}

# 5. Amazon MSK (Managed Streaming for Apache Kafka)
# Mensageria para desacoplar o Motor de Compras
resource "aws_msk_cluster" "kafka_itau_poc" {
  cluster_name           = "compra-programada-kafka"
  kafka_version          = "3.5.1"
  number_of_broker_nodes = 3

  broker_node_group_info {
    instance_type   = "kafka.t3.small"
    client_subnets  = ["subnet-xyz1", "subnet-xyz2", "subnet-xyz3"]
    security_groups = ["sg-kafka-access"]
  }

  encryption_info {
    encryption_in_transit {
      client_broker = "TLS"
    }
  }

  tags = {
    Environment = "Homologacao"
    Domain      = "Investimentos"
  }
}