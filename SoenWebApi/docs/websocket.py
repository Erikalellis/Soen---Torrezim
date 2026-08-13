#!/usr/bin/env python3
"""Exemplo: conectar no WebSocket e escutar eventos em tempo real"""
import json, threading, os

try:
    import websocket
except ImportError:
    import subprocess, sys
    subprocess.check_call([sys.executable, "-m", "pip", "install", "websocket-client"])
    import websocket

API = os.environ.get("API_URL", "http://localhost:3000")
KEY = os.environ.get("API_KEY", "")
WS_URL = API.replace("http://", "ws://").replace("https://", "wss://") + "/ws" + (f"?token={KEY}" if KEY else "")

def on_msg(ws, raw):
    data = json.loads(raw)
    event = data.get("event")
    payload = data.get("data")
    ts = data.get("timestamp")
    print(f"[{event}] {payload}")

def on_err(ws, err):
    print(f"WS erro: {err}")

def on_close(ws, code, msg):
    print(f"WS fechado ({code}): {msg}")

def on_open(ws):
    print("Conectado ao WebSocket! Escutando eventos...")

ws = websocket.WebSocketApp(WS_URL, on_message=on_msg, on_error=on_err, on_close=on_close, on_open=on_open)
ws.run_forever()
