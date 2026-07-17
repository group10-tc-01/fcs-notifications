# fcs-notifications

Worker .NET de e-mails transacionais da Conexão Solidária. Consome o tópico Kafka `email-notification-requested` e envia templates hospedados no Resend para boas-vindas, doação criada e doação processada.

## Operação

- `GET /health` expõe o estado operacional do host.
- `GET /metrics` expõe métricas Prometheus.
- O worker não tem banco próprio. Em falhas transitórias do Resend, a mensagem Kafka não é confirmada e será reprocessada.
- O envio usa a chave de idempotência `notification/<eventId>`.

## Configuração

Não versionar a chave da API. Para execução local, informe `Resend__ApiKey` ou `RESEND_API_KEY` no ambiente. Em produção, o Secret `notifications-runtime` é sincronizado pelo Infisical a partir de `/platform/resend-api-key`.

Publique três templates no Resend e configure seus IDs em `ResendTemplates__DonorWelcomeTemplateId`, `ResendTemplates__DonationCreatedTemplateId` e `ResendTemplates__DonationProcessedTemplateId`. Os dois templates de doação recebem as variáveis `donation_id` e `amount`; cada template define o próprio remetente, assunto e conteúdo.

Antes do deploy, verificar `flaviojcf.com.br` no Resend com os registros SPF/DKIM fornecidos e liberar `Conexão Solidária <notificacoes@flaviojcf.com.br>` como remetente.

## Local

```powershell
dotnet test
docker compose up --build
```
