#!/usr/bin/env python3
"""Exemplo: enviar mensagem de texto via API"""
import requests, sys

API = "http://localhost:3000"
KEY = ""  # ou leia de .env

num = sys.argv[1] if len(sys.argv) > 1 else "5511999999999"
msg = sys.argv[2] if len(sys.argv) > 2 else "Ola, tudo bem?"

hdr = {"x-api-key": KEY} if KEY else {}
r = requests.post(f"{API}/api/send/text", json={"number": num, "message": msg}, headers=hdr)
print(r.json())
