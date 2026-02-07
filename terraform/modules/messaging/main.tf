resource "aws_sns_topic" "this" {
  name = "${var.name_prefix}-topic"
  tags = var.tags
}

resource "aws_sqs_queue" "this" {
  name                       = "${var.name_prefix}-queue"
  visibility_timeout_seconds = 30
  message_retention_seconds  = 345600
  tags                       = var.tags
}

resource "aws_sns_topic_subscription" "sqs" {
  topic_arn = aws_sns_topic.this.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.this.arn
}

data "aws_iam_policy_document" "sqs_allow_sns" {
  statement {
    sid     = "Allow-SNS-SendMessage"
    effect  = "Allow"
    actions = ["sqs:SendMessage"]

    resources = [aws_sqs_queue.this.arn]

    principals {
      type        = "Service"
      identifiers = ["sns.amazonaws.com"]
    }

    condition {
      test     = "ArnEquals"
      variable = "aws:SourceArn"
      values   = [aws_sns_topic.this.arn]
    }
  }
}

resource "aws_sqs_queue_policy" "this" {
  queue_url = aws_sqs_queue.this.id
  policy    = data.aws_iam_policy_document.sqs_allow_sns.json
}
