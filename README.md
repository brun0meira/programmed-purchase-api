# 📈 Programmed Purchase API (POC Itaú Corretora)

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=.net)
![C#](https://img.shields.io/badge/C%23-Latest-239120?style=flat-square&logo=csharp)
![Next.js](https://img.shields.io/badge/Next.js-000000?style=flat-square&logo=next.js&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-8.0-0052CC?style=flat-square&logo=mysql)
![Apache Kafka](https://img.shields.io/badge/Apache%20Kafka-7.4.0-231F20?style=flat-square&logo=apache-kafka)
![Docker](https://img.shields.io/badge/Docker-Latest-2496ED?style=flat-square&logo=docker)
![Prometheus](https://img.shields.io/badge/Prometheus-Latest-E6522C?style=flat-square&logo=prometheus)
![Grafana](https://img.shields.io/badge/Grafana-F46800?style=flat-square&logo=grafana)

**Desafio Técnico - Itaú Corretora**  
*Sistema de Compra Programada de Ações com Arquitetura Event-Driven*

---

## 🔗 Links de Acesso (Ambiente de Produção)

A aplicação está hospedada em uma VPS, com infraestrutura 100% conteinerizada e Proxy Reverso configurado (Caddy) provendo HTTPS para todos os serviços.

- 🖥️ **Frontend (Painel Cliente / Admin):** https://programmed-purchase-web.vercel.app/  
- ⚙️ **API (Swagger):** https://api.desafioitau.online/swagger  
- 📊 **Observabilidade (Grafana):** https://grafana.desafioitau.online  (User: admin | Password: senha_itau)

### 🔐 Credenciais de Teste (Clientes na base)

Utilize os CPFs abaixo no Frontend para visualizar a rentabilidade e carteira gerada:

- `11122233344`
- `55566677788`
- `99900011122`

---

# 🎯 Visão Geral do Projeto

O **Programmed Purchase API** é um sistema robusto de investimento automatizado que permite aos clientes aderir a um plano de investimento recorrente em uma carteira recomendada de 5 ações (**Top Five**), definida pela equipe de Research da corretora.

### Fluxo Principal

1. **Adesão do Cliente** – Cliente informa dados cadastrais e valor mensal de aporte.  
2. **Compra Consolidada** – Motor de Compra executa compras fracionadas (1/3 do aporte).  
3. **Distribuição Proporcional** – Ações são distribuídas para a custódia individual de cada cliente.  
4. **Gestão Automática** – Sistema mantém preço médio de aquisição atualizado e calcula IR retido.

---

## Estrutura de Diretórios

```

programmed-purchase-api/
├── WebApi/                          # REST API (ASP.NET Core)
│   ├── Controllers/
│   │   ├── AdminController.cs
│   │   ├── ClientesController.cs
│   │   └── MotorController.cs
│   ├── cotacoes/
│   ├── Program.cs
│   ├── Startup.cs
│   ├── appsettings.json
│   └── WebApi.csproj
│
├── WorkerService/                   # Background Service (.NET Worker)
│   ├── IrConsumerWorker.cs
│   ├── Program.cs
│   └── WorkerService.csproj
│
├── Domain/                          # Lógica de Negócio (Clean Architecture)
│   ├── Business/
│   │   ├── MotorCompraService.cs    # Motor de compra programada
│   │   ├── CestaService.cs          # Gestão de cestas Top Five
│   │   ├── ClienteService.cs        # Gestão de clientes
│   │   └── ContaMasterService.cs    # Gerência de conta consolidada
│   ├── Entities/                    # Modelos de domínio
│   ├── Dto/                         # Data Transfer Objects
│   ├── Enum/                        # Enumerações
│   ├── Repositories/                # Interfaces de repositório
│   └── Domain.csproj
│
├── Infrastructure.Data/             # Acesso a Dados (EF Core)
│   ├── AppDbContext.cs
│   ├── Migrations/
│   ├── Repositories/                # Implementações de repositório
│   └── Infrastructure.Data.csproj
│
├── Infrastructure.Queue/            # Integração com Kafka
│   ├── KafkaProducerService.cs
│   └── Infrastructure.Queue.csproj
│
├── Tests/                           # Testes
│   ├── Controllers/
│   ├── Services/
│   └── Tests.csproj
│
├── docker-compose.yml               # Orquestração (Kafka, MySQL, Prometheus)
├── Dockerfile.Api                   # Build WebApi
├── Dockerfile.Worker                # Build WorkerService
├── prometheus.yml                   # Configuração de métricas
├── programmed-purchase-api.slnx     # Solution file (.NET)
└── README.md                        # Este arquivo

```

---

# 🏗️ Arquitetura da Solução

A solução adota uma arquitetura **Event-Driven**, com componentes comunicando-se via **Apache Kafka**, garantindo resiliência e desacoplamento de processos pesados.

```text
┌──────────────────────┐    Compra/Distribuição    ┌──────────────────────┐
│                      │      + IR Dedo-Duro       │                      │
│   WebApi (REST)      │──────────► Kafka ◄────────│  WorkerService       │
│                      │           (1)             │                      │
│  - Controllers       │                           │  - IrConsumerWorker  │
│  - Motor de Compra   │    Consumo Assíncrono     │  - Cálculos Fiscais  │
│  - Parse B3          │          (2)              │  - Persistência DB   │
│                      │◄─────────────────────────┐ │                      │
└──────────────────────┘                          │ └──────────────────────┘
         │                                        │
         │ (3) EF Core + Clean Architecture       │
         ▼                                        │
┌─────────────────────────────────────────────────┴───────────────────┐
│                      MySQL Database                                 │
│  (Cliente, ContaMaster, ContaGrafica, Custodia, Cesta, EventosIR)  │
└─────────────────────────────────────────────────────────────────────┘
```

---

# 🚀 Diferenciais Entregues

Fugindo do escopo básico de testes locais, este projeto foi construído com mentalidade de **produção**.

## Interface Rica (Next.js + Tailwind)

Desenvolvimento de painéis separados para:

**Administrador**
- Gestão da cesta
- Custódia Master

**Cliente**
- Evolução histórica de patrimônio
- Rentabilidade do investimento

---

## Deploy em Nuvem (VPS Hostinger)

Toda a stack rodando em um **servidor Linux real**, simulando um ambiente de produção.

---

## Segurança na Borda

Utilização do **Caddy Server** como **Reverse Proxy**, com gestão automática de certificados **SSL/TLS**.

---

## Observabilidade

Métricas da aplicação (.NET 9) e da infraestrutura (Kafka/MySQL) coletadas pelo **Prometheus** e visualizadas em dashboards dinâmicos no **Grafana**.

---

# ⚙️ Funcionalidades Implementadas

## ✓ Motor de Compra Programada

**Agrupamento**
- Coleta clientes ativos
- Isola 1/3 do aporte para a data de execução

**Cálculo da Compra**
- Utiliza a cotação de fechamento extraída dos arquivos **COTAHIST (B3)**

**Resíduos da Master**
- Verifica saldo remanescente da conta da corretora antes de executar compra no mercado

---

## ✓ Parse de Arquivos COTAHIST (B3)

Leitura automática de arquivos `.TXT` no padrão B3, injetados na pasta:

```
/cotacoes
```

---

## ✓ Roteamento Inteligente de Ordens

Implementa a divisão obrigatória entre **Lote Padrão** e **Mercado Fracionário**.

### Exemplo

Compra de **350 ações de PETR4**

```
3 lotes de 100 -> PETR4
50 ações -> PETR4F (mercado fracionário)
```

---

## ✓ Rateio e Preço Médio de Aquisição

- Distribuição proporcional à representatividade financeira de cada cliente no bloco da compra
- Recalculo matemático rigoroso do **Preço Médio** para apuração futura de lucro

---

## ✓ Gestão de Impostos (Dedo-Duro + Kafka)

Cálculo de **0,005%** sobre o valor operado de cada cliente.

### Fluxo

**API**
- Atua como **Producer**
- Publica os eventos no tópico do Kafka

**WorkerService**
- Atua como **Consumer**
- Processa e persiste os eventos no **MySQL**

Tudo ocorre de forma **assíncrona**, sem impactar o tempo de resposta da API.

---

# 💡 Decisão Arquitetural: Motor de Rebalanceamento

O desafio exige que, ao alterar a cesta **Top Five**:

- ativos antigos sejam vendidos  
- imposto de **20% sobre lucros acima de R$20k** seja calculado  
- novos ativos sejam comprados  

Executar tudo de forma **síncrona em uma única requisição HTTP** seria um antipadrão arquitetural, pois poderia causar **timeout no painel do administrador** dependendo do volume de clientes.

---

## Solução Event-Driven

1. O administrador altera a cesta via API  
2. O `CestaService` identifica o delta de ativos  
3. A flag `RebalanceamentoDisparado = true` é acionada  
4. A API retorna **200 OK** imediatamente  

---

## Preparação para Escala

A arquitetura permite que o **Worker Service** processe em background:

- venda dos ativos antigos
- validação do limite de **R$20 mil**
- cálculo de lucro com **Preço Médio**
- recompra dos novos ativos

Tudo ocorre de forma **assíncrona e escalável**.

---

# 💻 Como Executar Localmente

## Pré-requisitos

- Docker
- Docker Compose

---

## 1. Clonar o repositório

```bash
git clone https://github.com/seu-usuario/seu-repositorio.git
cd seu-repositorio
```

---

## 2. Subir a infraestrutura

```bash
docker compose up -d --build
```

Este comando inicia:

- MySQL  
- Kafka  
- Zookeeper  
- Prometheus  
- WebApi  
- WorkerService  

---

## 3. Verificar containers ativos

```bash
docker ps
```

---

# 🔎 Acessos Locais

**Swagger API**

```
http://localhost:8080/swagger
```

**Métricas Prometheus**

```
http://localhost:8080/metrics
```

---

# 🧪 Testes e Qualidade

O projeto foca em **excelência de design de código**.

### Cobertura de Testes

Validação de lógicas críticas:

- rateio financeiro
- arredondamento de lotes da B3
- cálculo de preço médio

---

### Clean Code & SOLID

Separação clara de responsabilidades:

```
Controllers
   ↓
Services
   ↓
Repositories
   ↓
EF Core
```

---

### Injeção de Dependência

Uso de padrões modernos do **.NET 9**, incluindo:

- `IServiceScopeFactory` em **Background Services**
- prevenção de **memory leaks em Workers**