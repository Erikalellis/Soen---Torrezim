#!/bin/bash
# Exemplo: Enviar mensagem de texto via API
# Uso: ./enviar-texto.sh 5511999999999 "Ola, tudo bem?"

API="http://localhost:3000"
KEY=""  # deixe vazio se sem auth

NUM="${1:-5511999999999}"
MSG="${2:-Ola, tudo bem?}"

curl -s -X POST "$API/api/send/text" \
  -H "Content-Type: application/json" \
  ${KEY:+-H "x-api-key: $KEY"} \
  -d "{\"number\": \"$NUM\", \"message\": \"$MSG\"}" | jq .
