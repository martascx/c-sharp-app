# Buteo — Edge Collector & SOAR

> **Academias Natixis | Dev Stream** — Projeto de Avaliação do Módulo de C# / .NET
>
>
>
> **Domínio:** Cibersegurança, Telemetria de Infraestrutura Bancária e Gestão de Incidentes (SIEM/SOAR).

## 1. Visão Geral e Objetivo

O **Buteo** é uma plataforma de recolha de eventos de segurança (*Edge Collector*) e deteção automática de ameaças desenvolvida em **ASP.NET Core (.NET)** com persistência em **SQL Server** e painel de monitorização web.

A API recebe eventos de autenticação, telemetria de servidores e transações bancárias, persiste-os numa base de dados relacional através de **Entity Framework Core** e avalia-os em tempo real contra regras de deteção dinâmicas configuráveis na tabela `DetectionRule`. Sempre que uma regra é violada, é gerado automaticamente um incidente (`Incident`) pronto para triagem e resolução na interface web.

### Regras de Deteção Implementadas

Todas as regras são lidas dinamicamente da base de dados (`DetectionRule`) tendo em conta o estado `IsEnabled`, o limiar (`Threshold`) e a janela temporal (`TimeWindowMinutes`):

1. **Tentativas Repetidas de Login Falhadas (`FAILED_LOGIN`) —*****Critical*** Dispara quando são registadas **5 ou mais falhas de autenticação** provenientes do mesmo endereço IP numa janela de **10 minutos**. É acionada automaticamente através do endpoint de login.
2. **Transferência de Valor Elevado (`TRANSFER`) —*****Critical*** Sinaliza qualquer transferência bancária cujo montante ultrapasse o limite configurado (**10.000,00 €**), consultando a tabela `BankTransaction` ou o payload JSON do evento.
3. **Rajada de Transações por IP (`TRANSACTION_FREQUENCY`) —*****High*** Deteta comportamentos automatizados ou *bots* quando chegam **5 ou mais pedidos de transação** do mesmo endereço IP em **1 minuto**.
4. **Limite Horário por Cliente (`CLIENT_HOURLY_LIMIT`) —*****Medium*** Vigila o volume de operações por conta bancária (`SenderIBAN`), disparando alerta quando o mesmo cliente realiza **10 ou mais transações** nos últimos **60 minutos**.
5. **Inatividade de Servidor (`HOST_INACTIVITY`) —*****High*** Verifica, através de `DateTime` e `TimeSpan`, se algum servidor ativo (`HostStatus = true`) deixou de enviar sinal de vida (*ping*) há mais de **30 minutos**. Ao disparar, gera o incidente e altera o estado do servidor para inativo (`false`) para evitar alertas duplicados até que este volte a comunicar.

## 2. Stack Tecnológica e Arquitetura

- **Backend:** ASP.NET Core Web API (C# / .NET)
- **Base de Dados:** Microsoft SQL Server (`Banco`) com Entity Framework Core (`BancoDbContext`)
- **Segurança:** Validação de credenciais com *hashing* `BCrypt` (`BCrypt.Net`)
- **Frontend:** Aplicação web de gestão de incidentes, eventos, hosts e regras de deteção
- **Orquestração:** Docker & Docker Compose

### Estrutura do Projeto

```
c-sharp-app/
├── Buteo.EdgeCollector.Api/
│   ├── Data/               # BancoDbContext (Entity Framework Core)
│   ├── Dtos/               # Objetos de transferência de dados (ex: LoginRequest)
│   ├── Endpoints/          # Rotas REST (AuthEndpoints, HostEndpoints, EventEndpoints, IncidentEndpoints)
│   ├── Models/             # Entidades (Client, Host, BankTransaction, SecurityEvent, DetectionRule, Incident)
│   ├── Services/           # RuleEngineService e constantes de EventTypes
│   └── Program.cs          # Configuração de serviços, BD e mapeamento de endpoints
├── Buteo_ui/               # Interface web de monitorização (Dashboard, Hosts, Incidents, Rules)
├── bdqueries.sql           # Script de criação da BD, tabelas, índices e dados iniciais
├── demo-tests.sh           # Script interativo para demonstração das regras bancárias
└── docker-compose.yml      # Orquestração dos contentores (SQL Server, API e Frontend)
```

## 3. Principais Endpoints da API

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Autentica o administrador (`BCrypt`). Em caso de falha, regista evento `FAILED_LOGIN` e avalia a regra de força bruta. |
| `GET` | `/api/hosts` | Lista os servidores monitorizados e verifica automaticamente se algum ultrapassou o tempo limite de inatividade (`HOST_INACTIVITY`). |
| `POST` | `/api/hosts/{id}/ping` | Recebe o *heartbeat* de um servidor, atualiza o `Timestamp` e repõe `HostStatus = true`. |
| `POST` | `/api/events` | Ingere um evento de segurança (`SecurityEvent`) e invoca o `RuleEngineService` para avaliação das regras ativas. |
| `GET` | `/api/incidents` | Lista os incidentes gerados para visualização no painel **Incident management**. |
| `PATCH` | `/api/incidents/{id}/status` | Atualiza o estado de um incidente (`New`, `Investigating`, `Resolved`). |

## 4. Como Executar o Projeto

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e em execução.
- Cliente SQL (ex.: extensão **SQL Server (mssql)** no VS Code) para executar o script inicial da base de dados.

### Passos de Execução

1. **Clonar o repositório:**

   ```
   git clone https://github.com/martascx/c-sharp-app.git
   cd c-sharp-app
   ```
2. **Construir e arrancar os contentores (SQL Server, API e Frontend):**

   ```
   docker compose up -d --build
   ```
3. **Inicializar a base de dados:**
   - Ligar ao SQL Server em `localhost,1433` (utilizador `sa` e palavra-passe definida no `docker-compose.yml`).
   - Executar o ficheiro `bdqueries.sql` para criar a base de dados `Banco`, as tabelas e os registos iniciais.
4. **Aceder à aplicação:**
   - **Frontend (Painel Web):** `http://localhost:4200`
   - **API Backend:** `http://localhost:5000`

## 5. Roteiro de Demonstração

Para apresentar todas as regras em tempo real no painel **Incident management**:

1. **Força Bruta (`FAILED_LOGIN`):** No ecrã de login do frontend, falhar a palavra-passe 5 vezes seguidas e entrar à 6.ª tentativa com um utilizador válido (ex.: `John Smith`). Confirmar o alerta crítico no separador **Incidents**.
2. **Inatividade de Servidor (`HOST_INACTIVITY`):** Recuar o `Timestamp` de um servidor ativo na tabela `Host` em 60 minutos via SQL e abrir o separador **Hosts** no browser. O servidor passa a inativo e o alerta surge na fila de incidentes.
3. **Regras Bancárias (`TRANSFER`, `TRANSACTION_FREQUENCY` e `CLIENT_HOURLY_LIMIT`):** Executar o guião interativo no terminal (Git Bash):

   ```
   chmod +x demo-tests.sh
   ./demo-tests.sh
   ```
   A cada passo, atualizar o painel **Incidents** para observar a criação dos alertas com os respetivos níveis de severidade e testar a alteração de estado (`New` → `Investigating` → `Resolved`).

## 6. Equipa e Organização

| Elemento | Responsabilidade Principal |
| --- | --- |
| **Afonso Cruz** | Motor de Regras (`RuleEngineService`), Endpoints de Monitorização e Testes |
| **Marta Carvalho** | Base de Dados (`SQL Server`), Interface Web (`Buteo_ui`)|
| **João Magano** | Interface Web (`Buteo_ui`) Integração Frontend-Backend e Endpoints de Autenticação/AP |
| **Artur Oliveira* | Orquestração Docker, Backend, Endpoints e Models |
