# PagueVeloz Transaction Processor

Sistema desenvolvido como parte do desafio técnico da **PagueVeloz**, simulando um ambiente de **alta disponibilidade e processamento financeiro** com múltiplos tipos de transações, controle de concorrência e consistência de dados.

---

## 📘 Sumário

1. Visão Geral
2. Arquitetura e Decisões Técnicas
3. Camadas e Padrões Utilizados
4. Resiliência e Confiabilidade
5. Configuração e Execução
6. Testes Unitários e de Integração
7. Exemplos de Uso da API
8. Documentação da API (Swagger)
9. Considerações Finais

---

## 🧭 Visão Geral

O **Transaction Processor** é responsável pelo gerenciamento de **contas e transações financeiras**, garantindo integridade, idempotência e isolamento transacional entre operações concorrentes.

### Principais Funcionalidades

* Criação de clientes e contas vinculadas
* Processamento de **crédito, débito, reserva, captura, estorno e transferência**
* Controle de **saldo disponível, saldo reservado e limite de crédito**
* Garantia de **idempotência** via `reference_id`
* Persistência e consistência transacional via **Unit of Work**
* **Polly** para políticas de retry e resiliência em acesso a banco de dados

---

## 🧱 Arquitetura e Decisões Técnicas

O projeto segue uma arquitetura em **Clean Architecture**, dividindo claramente responsabilidades entre **domínio, aplicação e infraestrutura**.

| Camada             | Responsabilidade                                                             |
| ------------------ | ---------------------------------------------------------------------------- |
| **Domain**         | Entidades, enums e regras de negócio puras (ex: `Account`, `Transaction`)    |
| **Application**    | Casos de uso e orquestração de lógica de aplicação (ex: *Handlers* Mediator) |
| **Infrastructure** | Acesso a banco (EF Core), persistência e configuração de serviços            |
| **API**            | Exposição dos endpoints HTTP, documentação Swagger/OpenAPI                   |

### 🧩 Tecnologias Principais

* **.NET 9** — Base do projeto
* **Entity Framework Core 9 (PostgreSQL)** — ORM e controle de transações
* **MediatR** — Implementação de CQRS e desacoplamento entre comandos/handlers
* **Polly** — Resiliência e retry com backoff exponencial
* **FluentAssertions + xUnit + Moq** — Testes unitários e integração
* **Swashbuckle (Swagger)** — Documentação e teste interativo da API

---

## 🧮 Camadas e Padrões Utilizados

### 🧱 Domain

Define as **entidades principais**:

* `Client`
* `Account`
* `Transaction`

As entidades encapsulam validações e comportamentos, por exemplo:

* `Account.Debit(amount)` — garante que saldo + crédito sejam suficientes.
* `Account.Reserve(amount)` — move valores do saldo disponível para o reservado.

### ⚙️ Application

Implementa os casos de uso via **CQRS com MediatR**:

* `CreateClientHandler` — cadastra um novo cliente.
* `CreateAccountHandler` — cria uma conta para um cliente existente.
* `CreateTransactionHandler` — processa operações financeiras conforme regras de negócio.

Utiliza o **Unit of Work** para garantir atomicidade:

```csharp
await _unitOfWork.BeginTransactionAsync();
await _accountRepository.UpdateAsync(account);
await _transactionRepository.AddAsync(transaction);
await _unitOfWork.CommitAsync();
```

### 🧰 Infrastructure

Responsável pela configuração do EF Core, mapeamento das entidades e **repositórios concretos**:

* `AccountRepository`
* `TransactionRepository`
* `ClientRepository`
* `UnitOfWork`

Inclui também a implementação de **Polly** via `RetryPolicyFactory`, com backoff exponencial:

```csharp
.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
```

---

## 🛡️ Resiliência e Confiabilidade

### 🔁 Retry e Backoff

Todas as operações de banco utilizam `Polly` para **repetição segura** em caso de falhas transitórias.

---

## ⚙️ Configuração e Execução

### 🧩 Compilar o projeto

```bash
dotnet build
```

### 🧪 Executar os testes

```bash
dotnet test
```

### 🚀 Executar a aplicação

```bash
dotnet run --project PagueVeloz.Api
```

### 🐳 Executar via Docker

```bash
docker-compose up --build
```

---

## 🧪 Testes Unitários e de Integração

### Testes Unitários

Cenários cobertos:

* Criação de cliente e conta
* Operações de transação (`credit`, `debit`, `reserve`, `capture`, `transfer`)
* Validação de saldo e limite de crédito
* Garantia de **idempotência** (`ReferenceId` duplicado)

Executar:

```bash
dotnet test
```

---

## 📡 Exemplos de Uso da API

### 🧍 Criar Cliente

```bash
curl -X POST http://localhost:5000/api/clients \
-H "Content-Type: application/json" \
-d '{"name": "Nome Cliente", "email": "nome.cliente@example.com"}'
```

### 🏦 Criar Conta

```bash
curl -X POST http://localhost:5000/api/accounts \
-H "Content-Type: application/json" \
-d '{"clientId": "CLI-001", "initialBalance": 0, "creditLimit": 50000}'
```

### 💰 Realizar Crédito

```bash
curl -X POST http://localhost:5000/api/transactions \
-H "Content-Type: application/json" \
-d '{
  "operation": "credit",
  "accountId": "ACC-001",
  "amount": 10000,
  "currency": "BRL",
  "referenceId": "TXN-001",
  "metadata": { "description": "Depósito inicial" }
}'
```

### ✅ Exemplo de Resposta

```json
{
  "transaction_id": "TXN-001-PROCESSED",
  "status": "success",
  "balance": 10000,
  "reserved_balance": 0,
  "available_balance": 10000,
  "timestamp": "2025-07-07T20:05:00Z",
  "error_message": null
}
```

---

## 📘 Documentação da API (Swagger)

Após rodar a aplicação:

* Acesse **[https://localhost:5001/swagger](https://localhost:5001/swagger)**
* Ou **[http://localhost:5000/swagger](http://localhost:5000/swagger)**

A documentação exibe todos os endpoints, schemas de requisição e resposta, e exemplos práticos.

---

## ⚖️ Considerações Finais

O projeto foi desenvolvido com foco em:

* **Isolamento transacional e integridade financeira**
* **Escalabilidade e concorrência segura**
* **Observabilidade e logging detalhado**
* **Testabilidade e cobertura ampla**

### Pontos de Extensão

* Publicação de eventos via fila (Kafka/RabbitMQ)
* Cache distribuído para leitura de contas
* Monitoramento via OpenTelemetry
* Autenticação e autorização via JWT

