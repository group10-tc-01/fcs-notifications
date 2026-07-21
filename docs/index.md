# FCS Notifications

O FCS Notifications consome eventos Kafka e envia notificações transacionais aos doadores por e-mail.

## Operação

- Namespace Kubernetes: `fcs-notifications`
- Health check: `/health`
- Métricas: `/metrics`
- Tópico Kafka: `email-notification-requested`

Consulte o [README](../README.md) para requisitos, execução local, contratos de evento e comportamento de idempotência.
