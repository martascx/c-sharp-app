#!/bin/bash

API_URL="http://localhost:5000/api"

echo "===================================================="
echo "  DEMONSTRAÇÃO BUTEO - MOTOR DE REGRAS DE SEGURANÇA"
echo "===================================================="
echo "Antes de continuar, garante que:"
echo "1. O painel web está aberto em http://localhost:4200"
echo "2. As tabelas Incident e SecurityEvent foram limpas no SQL Server"
read -p "Prime ENTER para começar..."

# ----------------------------------------------------
# TESTE 1: Transferência de Valor Elevado (> 10.000 €)
# ----------------------------------------------------
echo ""
echo "[1/3] A simular transferência suspeita de 15.000 €..."
curl -s -X POST "$API_URL/events" \
  -H "Content-Type: application/json" \
  -d '{
    "hostID": 1,
    "transactionID": null,
    "eventType": "TRANSFER",
    "rawPayload": "{\"amount\": 15000}",
    "ipAddress": "203.0.113.99"
  }' > /dev/null

echo "-> Evento enviado! Atualiza a página 'Incidents' no browser para ver o alerta."
read -p "Prime ENTER para passar à regra seguinte..."

# ----------------------------------------------------
# TESTE 2: Rajada de Transações (5 pedidos em 1 min)
# ----------------------------------------------------
echo ""
echo "[2/3] A simular 5 pedidos seguidos do mesmo IP (198.51.100.77)..."
for i in {1..5}; do
  curl -s -X POST "$API_URL/events" \
    -H "Content-Type: application/json" \
    -d "{
      \"hostID\": 1,
      \"transactionID\": null,
      \"eventType\": \"TRANSACTION_FREQUENCY\",
      \"rawPayload\": \"{\\\"step\\\": $i}\",
      \"ipAddress\": \"198.51.100.77\"
    }" > /dev/null
  echo "   Pedido $i enviado..."
done

echo "-> Rajada concluída! Atualiza a página 'Incidents' no browser."
read -p "Prime ENTER para passar à última regra..."

# ----------------------------------------------------
# TESTE 3: Limite Horário por Cliente (IBAN)
# ----------------------------------------------------
echo ""
echo "[3/3] A testar limite horário por cliente..."
read -p "Cola aqui o TransactionID que criaste no SQL Server (ex: 15) e prime ENTER: " TX_ID

curl -s -X POST "$API_URL/events" \
  -H "Content-Type: application/json" \
  -d "{
    \"hostID\": 1,
    \"transactionID\": $TX_ID,
    \"eventType\": \"CLIENT_HOURLY_LIMIT\",
    \"rawPayload\": \"{\\\"check\\\": \\\"hourly\\\"}\",
    \"ipAddress\": \"192.168.1.10\"
  }" > /dev/null

echo "-> Evento enviado! Atualiza a página 'Incidents' no browser para ver o último alerta."
echo ""
echo "===================================================="
echo "  DEMONSTRAÇÃO CONCLUÍDA COM SUCESSO!"
echo "===================================================="