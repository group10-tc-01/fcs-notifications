# fcs-notifications

Worker .NET de notificações transacionais da plataforma **Conexão Solidária**. Consome eventos Kafka e solicita ao Resend o envio de e-mails baseados em templates hospedados no próprio provedor.

> Microsserviço que compõe o MVP da Conexão Solidária junto a fcs-identity, fcs-donations, fcs-donation-worker, fcs-audit-logs, fcs-bff e fcs-web.

---

## Responsabilidades

- Consumir o tópico Kafka **email-notification-requested** no grupo **fcs-notifications-consumer-group**.
- Enviar o e-mail de boas-vindas após o cadastro de um doador.
- Enviar as confirmações de doação criada e de doação processada.
- Resolver o template correto do Resend sem acoplar os serviços de negócio ao provedor de e-mail.
- Garantir idempotência do envio com a chave **notification/<eventId>**.
- Retentar falhas transitórias sem confirmar o offset Kafka; descartar e registrar falhas permanentes sem expor e-mail ou outro dado pessoal nos logs.

O worker não possui banco próprio e não armazena o endereço de e-mail recebido no evento.

Documentação completa da arquitetura: [group10-tc-01/fcs-fase05-docs](https://github.com/group10-tc-01/fcs-fase05-docs).

---

## Perfis e Roles

O worker não autentica usuários nem aplica RBAC: ele é um componente interno da plataforma e só consome eventos Kafka. A autorização para publicar os eventos permanece nos serviços produtores; o acesso operacional ao pod é controlado pelo Kubernetes.

---

## Endpoints

O worker não expõe endpoints de negócio. Os únicos endpoints públicos são operacionais:

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | /health | Operacional | Healthcheck usado pelo K3s e pelo deploy. |
| GET | /metrics | Operacional | Métricas Prometheus e OpenTelemetry. |

---

## Contrato de evento

Os produtores — fcs-identity, fcs-donations e fcs-donation-worker — publicam **EmailNotificationRequestedEvent** no tópico **email-notification-requested**.

| Campo | Tipo | Descrição |
|---|---|---|
| eventId | UUID | Identificador usado na idempotência do Resend. |
| type | string | DonorWelcome, DonationCreated ou DonationProcessed. |
| recipientEmail | string | Destinatário do e-mail; não é registrado nos logs. |
| donationId | UUID opcional | Identificador da doação, quando aplicável. |
| amount | decimal opcional | Valor da doação, quando aplicável. |
| occurredAt | UTC datetime | Momento de ocorrência do evento. |

Exemplo de evento de doação processada:

~~~json
{
  "eventId": "4ed0257c-0293-4fe1-9c2c-3f5d9eca2b89",
  "type": "DonationProcessed",
  "recipientEmail": "doador@example.com",
  "donationId": "1fe24b6c-9e1f-4755-836c-a9970f3b78ba",
  "amount": 50.00,
  "occurredAt": "2026-07-17T12:00:00Z"
}
~~~

---

### Fluxo principal do worker

~~~mermaid
sequenceDiagram
    autonumber
    participant Producer as Serviço produtor
    participant Kafka as Kafka
    participant Worker as fcs-notifications
    participant Resend as Resend

    Producer->>Kafka: EmailNotificationRequestedEvent
    Kafka->>Worker: Consumir evento
    Worker->>Worker: Validar payload e selecionar template
    Worker->>Resend: Enviar com Idempotency-Key
    alt envio aceito
        Resend-->>Worker: Sucesso
        Worker->>Kafka: Confirmar offset
    else falha transitória
        Resend-->>Worker: Erro transitório
        Worker->>Kafka: Reprocessar mensagem
    else falha permanente ou payload inválido
        Worker->>Worker: Registrar erro sem PII
        Worker->>Kafka: Confirmar offset
    end
~~~

---

## Estrutura do projeto

~~~
src/
  Fcs.Notifications.Application/       # Consumo Kafka, regras de processamento e Resend
  Fcs.Notifications.Worker/            # Host .NET, DI, observabilidade e endpoints operacionais
tests/
  Fcs.Notifications.UnitTests/         # Templates, idempotência, erros e processamento
k8s/                                   # Recursos proprietários do worker
assets/email/                          # Imagens de apoio para os templates do Resend
~~~
---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) e Docker Compose
- Uma API key e os IDs de templates do Resend para executar o envio localmente
- Portas livres: 5007 (worker), 9092 (Kafka), 8081 (Kafka UI), 5341 e 5342 (Seq)

---

## Subindo o ambiente local

O compose deste repositório sobe o worker, Kafka, Kafka UI e Seq. Para o ambiente completo integrado da Conexão Solidária, utilize o repositório fcs-infra.

### 1. Criar a rede de observabilidade

~~~bash
docker network create fcs-observability
~~~

Esse comando só é necessário uma vez na máquina.

### 2. Informar a configuração do Resend

~~~powershell
$env:RESEND_API_KEY = "<api-key>"
$env:RESEND_DONOR_WELCOME_TEMPLATE_ID = "<template-id>"
$env:RESEND_DONATION_CREATED_TEMPLATE_ID = "<template-id>"
$env:RESEND_DONATION_PROCESSED_TEMPLATE_ID = "<template-id>"
~~~

### 3. Subir os serviços

~~~bash
docker compose up --build
~~~

URLs úteis:

- Worker: http://localhost:5007/health
- Métricas: http://localhost:5007/metrics
- Kafka UI: http://localhost:8081
- Seq: http://localhost:5342

Para executar apenas o worker pelo SDK:

~~~bash
dotnet restore
dotnet run --project src/Fcs.Notifications.Worker
~~~

---

## Testes

~~~bash
dotnet test Fcs.Notifications.slnx --configuration Release
~~~

Os testes cobrem a seleção de templates, idempotência, retries, falhas permanentes e a ausência de PII nos logs. A esteira exige cobertura mínima de **80%**.

---

## Observabilidade

- Logs estruturados com **Serilog** e Seq no ambiente local.
- **OpenTelemetry** para tracing e métricas.
- Endpoints operacionais:
  - GET /health
  - GET /metrics

Na VPS, o Datadog coleta métricas e health checks por Autodiscovery dentro da rede do pod. Os endpoints também ficam disponíveis via Ingress operacional com TLS:

- https://fcs-notifications.flaviojcf.com.br/health
- https://fcs-notifications.flaviojcf.com.br/metrics

---

## CI/CD

Os workflows em .github/workflows reutilizam as pipelines do repositório fcs-pipelines:

- branch-name-check.yml — política de nomes de branch.
- dotnet-service-ci.yml — build .NET, testes, cobertura, scans de segurança, validação Docker e publicação no GHCR na main.
- dotnet-service-delivery.yml — entrega manual da imagem imutável no K3s da VPS, protegida pelo environment production.

Gates principais: Gitleaks, scan de dependências, build, testes com cobertura mínima de 80%, validação Docker e publicação da imagem.

---

## Kubernetes

O k8s/kustomization.yaml aplica todos os recursos proprietários do worker: Deployment, ConfigMap, Service, RBAC, Ingress HTTPS, Certificate e InfisicalStaticSecret.

Os componentes compartilhados — Kafka, Traefik, cert-manager, Datadog e Infisical Operator — são gerenciados pelo fcs-infra. O namespace fcs-notifications também é criado e mantido por esse repositório.

### Secrets e variables do deploy

Configure os secrets no repositório GitHub. O environment production é o gate de aprovação do deploy:

| Tipo | Nome | Uso |
|---|---|---|
| Secret | K3S_KUBECONFIG | Kubeconfig usado para aplicar recursos no namespace. |
| Secret | VPS_DEPLOY_SSH_KEY | Chave privada do usuário de deploy para abrir o túnel SSH. |
| Secret | VPS_KNOWN_HOSTS | Host key da VPS, obtida por canal confiável. |
| Variable | VPS_HOST | IP ou hostname da VPS. |
| Variable | VPS_DEPLOY_USER | Usuário de deploy da VPS. |

No Infisical, crie **resend-api-key** no projeto **fcs-platform-dd-uk**, ambiente **prod**, caminho **/platform**. O Operator sincroniza esse valor para notifications-runtime.

---

## Banco de dados

O fcs-notifications não possui banco de dados. O Kafka mantém a entrega dos eventos e o Resend aceita a chave de idempotência para evitar envios duplicados quando o worker reprocessa uma mensagem.

---

## Como este serviço atende ao hackathon

| Requisito do hackathon | Onde é atendido |
|---|---|
| Notificação ao doador | E-mail de boas-vindas e confirmações de doação via Resend. |
| Integração assíncrona entre microsserviços | Tópico Kafka email-notification-requested. |
| Confiabilidade no processamento | Commit de offset somente após sucesso e idempotência por eventId. |
| Endpoint /health e /metrics | Expostos pelo worker e monitorados no K3s. |
| Imagem Docker e pipeline | Dockerfile e workflows em .github/workflows. |
| Microsserviço distinto | fcs-notifications separado dos serviços de identidade e doações. |
