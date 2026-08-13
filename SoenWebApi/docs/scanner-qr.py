#!/usr/bin/env python3
"""Exemplo: escutar WebSocket e exibir QR Code automaticamente"""
import json, time, shutil, os

try:
    import websocket
except ImportError:
    import subprocess, sys
    subprocess.check_call([sys.executable, "-m", "pip", "install", "websocket-client"])
    import websocket

try:
    import qrcode
    QR_OK = True
except ImportError:
    QR_OK = False

API = os.environ.get("API_URL", "http://localhost:3000")
KEY = os.environ.get("API_KEY", "")
WS_URL = API.replace("http://", "ws://").replace("https://", "wss://") + "/ws" + (f"?token={KEY}" if KEY else "")"

def on_msg(ws, raw):
    data = json.loads(raw)
    event = data.get("event")
    payload = data.get("data", {})
    if event == "qr":
        cid = payload.get("client_id")
        qr_text = payload.get("qr")
        print(f"\n=== QR CODE para [{cid}] ===")
        if QR_OK:
            qr = qrcode.make(qr_text)
            qr.show()
        else:
            print(f"QR Texto: {qr_text[:80]}...")
    elif event == "status":
        cid = payload.get("client_id")
        if payload.get("connected"):
            print(f"\n[{cid}] Conectado!")
        else:
            print(f"\n[{cid}] Desconectado")
    elif event == "message":
        print(f"\n[{payload.get('client_id')}] MSG de {payload.get('from')}: {payload.get('body', '')[:60]}")

ws = websocket.WebSocketApp(WS_URL, on_message=on_msg)
ws.run_forever()
