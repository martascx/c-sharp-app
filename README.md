# Buteo — Edge Collector & SOAR API (.NET)

> **Academias Natixis | Dev Stream** — Projeto de Avaliação do Módulo de C# / .NET
>
>
>
> **Domínio:** Cibersegurança, Telemetria de Infraestrutura e Resposta Automatizada a Incidentes (SIEM/SOAR).

## 1. Visão Geral e Objetivo

O **Buteo Edge Collector API** é um motor de ingestão de telemetria e correlação de segurança desenvolvido em **ASP.NET Core (.NET 10)**. A aplicação atua como um coletor central que recebe eventos de segurança enviados por agentes de borda (ou simuladores HTTP), armazena-os em memória através de estruturas `List<T>` protegidas contra concorrência e aplica regras automáticas de deteção de ameaças em tempo real.

### Regras de Correlação Implementadas

- **Ataque de Força Bruta (`AUTH_FAIL`):** Dispara automaticamente um incidente (`HIGH`) quando são registadas **5 ou mais falhas de autenticação** provenientes do mesmo endereço IP de origem (`sourceIp`).
- **Escalada de Privilégios (`PRIVILEGE_ESCALATION`):** Gera imediatamente um incidente (`CRITICAL`) perante qualquer tentativa de alteração de permissões administrativas no nó monitorizado.
- **Anomalia Transacional Bancária (`BANK_TX`):** Sinaliza um incidente (`CRITICAL`) quando é processada uma operação financeira com montante superior a **10.000,00 €**, alterando o estado do servidor afetado para `UNDER_ATTACK`.

## 2. Stack Tecnológica e Arquitetura

- **Framework:** .NET 10 (`ASP.NET Core Web API`)
- **Persistência:** Estruturas `List<T>` em memória geridas por um serviço `Singleton` com sincronização via `lock` (*Thread-Safety*).
- **Serialização & Validação:** `System.Text.Json` e *Model Binding* nativo.
- **Testes & Documentação:** OpenAPI (`/openapi/v1.json`) e ficheiro de pedidos `.http` integrado no IDE.

### Estrutura de Diretórios

```
Buteo.EdgeCollector/
├── Buteo.EdgeCollector.sln
└── Buteo.EdgeCollector.Api/
    ├── Models/          # Entidades de domínio (Host, SecurityEvent, Incident)
    ├── Data/            # Repositório Singleton em memória (List<T> + lock)
    ├── Services/        # Motor de correlação e regras de deteção
    ├── Endpoints/       # Definição das rotas REST e validação de códigos HTTP
    ├── Program.cs       # Configuração do pipeline HTTP e Injeção de Dependências
    └── *.http           # Coleção de pedidos para simulação de ataques
```

## 3. Endpoints da API

| Método | Rota | Descrição | Códigos HTTP |
| --- | --- | --- | --- |
| `POST` | `/api/hosts` | Regista um novo servidor/nó monitorizado no inventário. | `201`, `400`, `409` |
| `GET` | `/api/hosts` | Lista todos os hosts registados e o respetivo estado operacional. | `200` |
| `POST` | `/api/events` | Ingere um evento de telemetria e executa o motor de correlação. | `201`, `400`, `404` |
| `GET` | `/api/hosts/{id}/events` | Consulta o histórico cronológico de eventos de um host específico. | `200`, `404` |
| `GET` | `/api/incidents` | Lista todos os alertas de segurança gerados automaticamente. | `200` |
| `PATCH` | `/api/incidents/{id}/mitigate` | Marca um incidente como mitigado e atualiza o estado do host. | `200`, `404`, `409` |

## 4. Como Executar o Projeto

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) instalado.

### Passos de Execução

1. **Clonar o repositório:**

   ```
   git clone https://github.com/martascx/c-sharp-app.git
   cd Buteo.EdgeCollector
   ```
2. **Compilar a solução:**

   ```
   dotnet build
   ```
3. **Arrancar a API:**

   ```
   dotnet run --project Buteo.EdgeCollector.Api
   ```
   *(Nota para ambientes Windows com políticas restritivas WDAC/AppLocker: o projeto está configurado com `<UseAppHost>false</UseAppHost>` no `.csproj` para executar diretamente a `.dll` gerida através do runtime `dotnet.exe` sem gerar executáveis `.exe` não assinados).*

## 5. Roteiro de Demonstração (Simulação de Ataques)

1. Enviar `POST /api/hosts` para registar um nó (ex.: `srv-core-banking-01`) e copiar o `id` (`Guid`) devolvido.
2. Executar uma rajada de 5 pedidos `POST /api/events` com `"eventType": "AUTH_FAIL"` e o mesmo `"sourceIp"`.
3. Consultar `GET /api/incidents` para verificar a criação automática do alerta de Força Bruta e `GET /api/hosts` para confirmar a alteração do estado do servidor para `UNDER_ATTACK`.
4. Executar `PATCH /api/incidents/{incidentId}/mitigate` para simular a resposta do analista SOC e repor a normalidade operacional.

## 6. Equipa e Organização

- **Membro 1:** Camada de Domínio (`Models/`) e Repositório *Thread-Safe* (`Data/`).
- **Membro 2:** Endpoints de Gestão de Hosts e Mitigação de Incidentes.
- **Membro 3:** Ingestão de Telemetria e Motor de Regras de Correlação (`Services/`).
- **Membro 4:** Gestão de Repositório/Kanban, Simulação de Testes (`.http`) e Relatório Técnico.
