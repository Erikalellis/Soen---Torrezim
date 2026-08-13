"""
DDS: ZAP-API v3.0 - Painel de Controle para WhatsApp Bot
Conecta ao servidor Node.js via HTTP + WebSocket (tempo real)
"""

import tkinter as tk
from tkinter import ttk, messagebox, scrolledtext, filedialog, simpledialog
import requests
import json
import threading
import time
import os
import re
import subprocess
import sys
import webbrowser
from datetime import datetime
from pathlib import Path

try:
    from PIL import Image, ImageTk
    import qrcode
    QR_OK = True
except ImportError:
    QR_OK = False

try:
    import websocket
    WS_OK = True
except ImportError:
    WS_OK = False

try:
    import tkinterweb
    WEBVIEW_OK = True
except ImportError:
    WEBVIEW_OK = False

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
API_BASE = "http://localhost:3000"
WS_BASE = API_BASE.replace("http://", "ws://").replace("https://", "wss://") + "/ws"
API_KEY = ""
WS_CONNECTED = [False]
WS_APP = [None]
NODE_PROCESS = [None]
SERVER_RUNNING = [False]
ZAP_APP = [None]  # Referencia global para ZapGUI instance

env_path = os.path.join(BASE_DIR, '.env')

def _load_env():
    global API_KEY, HDR
    API_KEY = ""
    if os.path.exists(env_path):
        with open(env_path, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if line.startswith('API_KEY='):
                    v = line.split('=', 1)[1].strip().strip('"').strip("'")
                    if v: API_KEY = v
    HDR = {"x-api-key": API_KEY} if API_KEY else {}

_load_env()


# ================================================================
# Configuracao inicial
# ================================================================
def _need_setup():
    if not os.path.exists(env_path):
        return True
    with open(env_path, 'r', encoding='utf-8') as f:
        content = f.read()
    if 'API_KEY=' in content:
        return False
    return True


def _run_setup(parent):
    """Janela de configuracao inicial"""
    w = tk.Toplevel(parent)
    w.title("Configuracao Inicial - DDS: ZAP-API")
    w.geometry("500x450")
    w.resizable(False, False)
    w.transient(parent)
    w.grab_set()

    frame = ttk.Frame(w, padding=20)
    frame.pack(fill=tk.BOTH, expand=True)

    ttk.Label(frame, text="Bem-vindo ao DDS: ZAP-API!", font=("Segoe UI", 16, "bold")).pack(anchor=tk.W, pady=(0, 16))
    ttk.Label(frame, text="Configure abaixo os dados principais. Todos opcionais exceto Porta.", font=("Segoe UI", 9), foreground="gray").pack(anchor=tk.W, pady=(0, 12))

    fields = {}
    for label, key, default, tip in [
        ("Porta do servidor:", "port", "3000", "Porta HTTP (padrao: 3000)"),
        ("Chave API (API_KEY):", "api_key", "", "Protege a API com header x-api-key"),
        ("Chave OpenAI (OPENAI_API_KEY):", "openai_key", "", "Deixe vazio se nao usar IA"),
        ("Modelo OpenAI:", "openai_model", "gpt-4o-mini", "Modelo padrao: gpt-4o-mini"),
        ("Provedor IA (aiProvider):", "ai_provider", "openai", "openai ou ollama"),
        ("URL Ollama:", "ollama_url", "http://localhost:11434", "URL do servidor Ollama local"),
        ("Modelo Ollama:", "ollama_model", "llama3", "Modelo Ollama (llama3, mistral, etc)"),
    ]:
        r = ttk.Frame(frame)
        r.pack(fill=tk.X, pady=4)
        ttk.Label(r, text=label, font=("Segoe UI", 9)).pack(anchor=tk.W)
        e = ttk.Entry(r, font=("Segoe UI", 10))
        e.pack(fill=tk.X, pady=(2, 0))
        e.insert(0, default)
        ttk.Label(r, text=tip, font=("Segoe UI", 8), foreground="gray").pack(anchor=tk.W)
        fields[label] = e

    err_lbl = ttk.Label(frame, text="", foreground="red", font=("Segoe UI", 9))
    err_lbl.pack(pady=(8, 0))

    def save():
        values = {k: e.get().strip() for k, e in fields.items()}
        port = values.get("Porta do servidor:", "3000")
        try:
            p = int(port)
            if p < 1 or p > 65535:
                err_lbl.configure(text="Porta invalida (1-65535)")
                return
        except:
            err_lbl.configure(text="Porta deve ser numero")
            return

        lines = [
            f"PORT={port}",
            f"API_KEY={values.get('Chave API (API_KEY):', '')}",
            f"BOT_NAME=DDS ZAP Bot",
            f"HUMAN_RESET_HOURS=24",
            f"MESSAGE_TTL_MINUTES=60",
            f"MAX_MESSAGES=500",
            f"RATE_LIMIT_WINDOW=15",
            f"RATE_LIMIT_MAX=100",
            f"",
            f"# Deixe vazio para auto-detect (vai encontrar o Chrome do sistema)",
            f"CHROME_PATH=",
            f"OPENAI_API_KEY={values.get('Chave OpenAI (OPENAI_API_KEY):', '')}",
            f"OPENAI_MODEL={values.get('Modelo OpenAI:', 'gpt-4o-mini')}",
            f"AI_PROVIDER={values.get('Provedor IA (aiProvider):', 'openai')}",
            f"OLLAMA_URL={values.get('URL Ollama:', 'http://localhost:11434')}",
            f"OLLAMA_MODEL={values.get('Modelo Ollama:', 'llama3')}",
        ]
        with open(env_path, 'w', encoding='utf-8') as f:
            f.write("\n".join(lines) + "\n")
        _load_env()
        w.destroy()
        messagebox.showinfo("Configuracao", ".env criado com sucesso!\n\nClique em 'Iniciar Servidor' para rodar.")

    ttk.Button(frame, text="Salvar e Continuar", command=save).pack(pady=(12, 0))


# ================================================================
# Server management
# ================================================================
def _update_btns():
    """Atualiza botoes e status bar conforme estado do servidor"""
    app = ZAP_APP[0]
    if not app: return
    if SERVER_RUNNING[0]:
        app.server_btn.configure(text="\u23f9 Parar Servidor")
        app.st_server.configure(text="Servidor: ON")
        app.restart_btn.configure(state=tk.NORMAL)
    else:
        app.server_btn.configure(text="\u25b6 Iniciar Servidor")
        app.st_server.configure(text="Servidor: OFF")
        app.restart_btn.configure(state=tk.DISABLED)

def _start_server(parent):
    """Inicia o servidor Node.js em background (usa o node embutido do pacote)"""
    if NODE_PROCESS[0]:
        messagebox.showinfo("Servidor", "Servidor ja esta rodando!")
        return

    # Usa o Node.js portatil embutido (nao depende de node instalado no sistema)
    node_exe = os.path.join(BASE_DIR, "node", "node.exe")
    if not os.path.exists(node_exe):
        messagebox.showerror("Erro", "Node.js embutido nao encontrado em:\n" + node_exe + "\nReinstale o pacote completo.")
        return

    try:
        proc = subprocess.Popen(
            [node_exe, "index.js"],
            cwd=BASE_DIR,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0,
            shell=False,
        )
        NODE_PROCESS[0] = proc
        SERVER_RUNNING[0] = True
        _update_btns()

        # Thread para ler output
        def reader():
            for line in iter(proc.stdout.readline, b''):
                if not line:
                    break
            proc.wait()
            SERVER_RUNNING[0] = False
            NODE_PROCESS[0] = None
            _update_btns()

        threading.Thread(target=reader, daemon=True).start()
        messagebox.showinfo("Servidor", "Servidor Node.js iniciado!\n\nAcesse http://localhost:3000/docs")
    except Exception as e:
        messagebox.showerror("Erro", f"Falha ao iniciar servidor:\n{e}")


def _kill_tree(pid):
    """Encerra o processo e sua arvore (mata tambem o Chromium filho do Puppeteer)."""
    if sys.platform == "win32":
        try:
            subprocess.run(["taskkill", "/PID", str(pid), "/T", "/F"],
                           capture_output=True, check=False)
        except Exception:
            pass
    else:
        try:
            os.killpg(os.getpgid(pid), 9)
        except Exception:
            pass


def _stop_server():
    if NODE_PROCESS[0]:
        try:
            _kill_tree(NODE_PROCESS[0].pid)
        except:
            pass
        NODE_PROCESS[0] = None
        SERVER_RUNNING[0] = False
        _update_btns()

def _restart_server(parent):
    """Reinicia o servidor Node.js"""
    _stop_server()
    time.sleep(1)
    _start_server(parent)


# ================================================================
# Janela inicial "Iniciar Servidor?"
# ================================================================
def _show_startup_dialog(root, app_ref):
    """Janela modal perguntando se quer iniciar o servidor"""
    result = {"start": False}

    d = tk.Toplevel(root)
    d.title("ZAP-API - Iniciar Servidor")
    d.geometry("420x280")
    d.resizable(False, False)
    d.transient(root)
    d.grab_set()

    # Centralizar
    d.update_idletasks()
    x = root.winfo_x() + (root.winfo_width() // 2) - 210
    y = root.winfo_y() + (root.winfo_height() // 2) - 140
    d.geometry(f"+{x}+{y}")

    frame = ttk.Frame(d, padding=30)
    frame.pack(fill=tk.BOTH, expand=True)

    ttk.Label(frame, text="🚀 DDS: ZAP-API v3.0", font=("Segoe UI", 18, "bold")).pack(anchor=tk.CENTER, pady=(0, 8))
    ttk.Label(frame, text="Painel de Controle WhatsApp", font=("Segoe UI", 10), foreground="gray").pack(anchor=tk.CENTER, pady=(0, 20))

    ttk.Label(frame, text="O servidor Node.js esta offline.", font=("Segoe UI", 10)).pack(anchor=tk.CENTER, pady=(0, 4))
    ttk.Label(frame, text="Deseja iniciar o servidor agora?", font=("Segoe UI", 10)).pack(anchor=tk.CENTER, pady=(0, 20))

    bf = ttk.Frame(frame)
    bf.pack(anchor=tk.CENTER)

    def on_sim():
        result["start"] = True
        d.destroy()

    def on_nao():
        result["start"] = False
        d.destroy()

    ttk.Button(bf, text="  Sim  ", width=12, command=on_sim).pack(side=tk.LEFT, padx=8)
    ttk.Button(bf, text="  Nao  ", width=12, command=on_nao).pack(side=tk.LEFT, padx=8)

    d.grab_set()
    root.wait_window(d)

    if result["start"]:
        _start_server(root)


# ================================================================
# WebSocket connection (runs in background thread)
# ================================================================
def _ws_connect():
    while True:
        try:
            ws = websocket.WebSocketApp(
                WS_BASE,
                on_message=_ws_on_message,
                on_error=_ws_on_error,
                on_close=_ws_on_close,
                on_open=_ws_on_open,
            )
            ws.run_forever(ping_interval=30, ping_timeout=10)
        except:
            pass
        WS_CONNECTED[0] = False
        time.sleep(5)


def _ws_on_open(ws):
    WS_CONNECTED[0] = True


def _ws_on_close(ws, code, msg):
    WS_CONNECTED[0] = False


def _ws_on_error(ws, err):
    WS_CONNECTED[0] = False


def _ws_on_message(ws, raw):
    try:
        data = json.loads(raw)
        event = data.get("event")
        payload = data.get("data", {})
        app = WS_APP[0]
        if app:
            app.root.after(0, app._on_ws_event, event, payload)
    except:
        pass

def api_get(path, params=None):
    try:
        r = requests.get(f"{API_BASE}{path}", headers=HDR, params=params, timeout=5)
        if r.status_code == 401: return {"error": "API Key invalida"}
        return r.json() if r.ok else {"error": f"HTTP {r.status_code}: {r.text[:200]}"}
    except requests.ConnectionError:
        return {"error": "Servidor offline"}
    except Exception as e:
        return {"error": str(e)}

def api_post(path, data=None):
    try:
        r = requests.post(f"{API_BASE}{path}", json=data or {}, headers=HDR, timeout=10)
        return r.json() if r.ok else {"error": f"HTTP {r.status_code}: {r.text[:200]}"}
    except requests.ConnectionError:
        return {"error": "Servidor offline"}
    except Exception as e:
        return {"error": str(e)}

def api_delete(path, data=None):
    try:
        r = requests.delete(f"{API_BASE}{path}", json=data or {}, headers=HDR, timeout=5)
        return r.json() if r.ok else {"error": f"HTTP {r.status_code}: {r.text[:200]}"}
    except requests.ConnectionError:
        return {"error": "Servidor offline"}
    except Exception as e:
        return {"error": str(e)}

def api_put(path, data=None):
    try:
        r = requests.put(f"{API_BASE}{path}", json=data or {}, headers=HDR, timeout=5)
        return r.json() if r.ok else {"error": f"HTTP {r.status_code}: {r.text[:200]}"}
    except requests.ConnectionError:
        return {"error": "Servidor offline"}
    except Exception as e:
        return {"error": str(e)}

def api_patch(path, data=None):
    try:
        r = requests.patch(f"{API_BASE}{path}", json=data or {}, headers=HDR, timeout=5)
        return r.json() if r.ok else {"error": f"HTTP {r.status_code}: {r.text[:200]}"}
    except requests.ConnectionError:
        return {"error": "Servidor offline"}
    except Exception as e:
        return {"error": str(e)}

# ================================================================
    # INTERFACE PRINCIPAL
# ================================================================
class ZapGUI:
    def __init__(self, root):
        self.root = root
        ZAP_APP[0] = self
        self.root.title("DDS: ZAP-API v3.0 - Painel de Controle WhatsApp")
        self.root.geometry("1024x700")
        self.root.minsize(900, 600)

        self.style = ttk.Style()
        for t in self.style.theme_names():
            if t.lower() in ('vista', 'clam', 'winnative'):
                self.style.theme_use(t)
                break

        self._polling = True
        self._qr_images = {}
        self._config_data = None
        self._intents_data = []
        self._convs_data = {}

        self._build_ui()
        if WS_OK:
            WS_APP[0] = self
            threading.Thread(target=_ws_connect, daemon=True).start()
        self._start_polling()

    def _build_ui(self):
        m = ttk.Frame(self.root, padding=4)
        m.pack(fill=tk.BOTH, expand=True)

        top = ttk.Frame(m)
        top.pack(fill=tk.X)
        ttk.Label(top, text="DDS: ZAP-API - Gerenciador WhatsApp", font=("Segoe UI", 14, "bold")).pack(side=tk.LEFT)
        self.server_btn = ttk.Button(top, text="▶ Iniciar Servidor", command=lambda: _start_server(self.root), width=16)
        self.server_btn.pack(side=tk.LEFT, padx=2)
        self.restart_btn = ttk.Button(top, text="🔄 Reiniciar", command=lambda: _restart_server(self.root), width=12, state=tk.DISABLED)
        self.restart_btn.pack(side=tk.LEFT, padx=2)
        self.admin_btn = ttk.Button(top, text="🌐 Admin Web", command=lambda: webbrowser.open(f"{API_BASE}/admin/"), width=12)
        self.admin_btn.pack(side=tk.LEFT, padx=2)
        self.nav_btn = ttk.Button(top, text="📄 Docs", command=self._open_browser, width=12)
        self.nav_btn.pack(side=tk.LEFT)
        ttk.Label(top, text=f"API: {API_BASE}", font=("Segoe UI", 8), foreground="gray").pack(side=tk.RIGHT)

        nb = ttk.Notebook(m)
        nb.pack(fill=tk.BOTH, expand=True, pady=(4, 0))
        self.nb = nb

        self.t_conn = ttk.Frame(nb, padding=6)
        self.t_send = ttk.Frame(nb, padding=6)
        self.t_msg = ttk.Frame(nb, padding=6)
        self.t_config = ttk.Frame(nb, padding=6)
        self.t_tools = ttk.Frame(nb, padding=6)
        self.t_stats = ttk.Frame(nb, padding=6)
        self.t_browser = ttk.Frame(nb, padding=2)

        nb.add(self.t_conn, text="  Conexao  ")
        nb.add(self.t_send, text="  Enviar  ")
        nb.add(self.t_msg, text="  Mensagens  ")
        nb.add(self.t_config, text="  Configuracao  ")
        nb.add(self.t_tools, text="  Ferramentas  ")
        nb.add(self.t_stats, text="  Estatisticas  ")
        nb.add(self.t_browser, text="  Navegador  ")

        self._build_conn_tab()
        self._build_send_tab()
        self._build_msg_tab()
        self._build_config_tab()
        self._build_tools_tab()
        self._build_stats_tab()
        self._build_browser_tab()

        # Status bar
        sf = ttk.Frame(m)
        sf.pack(fill=tk.X, pady=(4, 0))
        self.st_dot = ttk.Label(sf, text="\u25cf", foreground="gray", font=("Segoe UI", 10))
        self.st_dot.pack(side=tk.LEFT, padx=(0, 4))
        self.st_label = ttk.Label(sf, text="Aguardando...", font=("Segoe UI", 9))
        self.st_label.pack(side=tk.LEFT)
        self.st_server = ttk.Label(sf, text="", font=("Segoe UI", 8), foreground="gray")
        self.st_server.pack(side=tk.RIGHT, padx=4)
        self.st_ws = ttk.Label(sf, text="", font=("Segoe UI", 8), foreground="gray")
        self.st_ws.pack(side=tk.RIGHT, padx=4)
        self.st_mode = ttk.Label(sf, text="", font=("Segoe UI", 8), foreground="gray")
        self.st_mode.pack(side=tk.RIGHT, padx=4)
        self.st_info = ttk.Label(sf, text="", font=("Segoe UI", 8), foreground="gray")
        self.st_info.pack(side=tk.RIGHT, padx=4)

    def _build_browser_tab(self):
        """Navegador embedded dentro do app"""
        bf = ttk.Frame(self.t_browser)
        bf.pack(fill=tk.BOTH, expand=True)

        toolbar = ttk.Frame(bf)
        toolbar.pack(fill=tk.X)

        self.browser_url = tk.StringVar(value=f"{API_BASE}/docs")
        ttk.Entry(toolbar, textvariable=self.browser_url, font=("Segoe UI", 9)).pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 4))
        ttk.Button(toolbar, text="Ir", command=self._browser_go, width=6).pack(side=tk.LEFT, padx=(0, 2))
        ttk.Button(toolbar, text="🔄", command=self._browser_go, width=4).pack(side=tk.LEFT)

        self.browser_frame = ttk.Frame(bf)
        self.browser_frame.pack(fill=tk.BOTH, expand=True, pady=(4, 0))

        self._browser_widget = None
        if WEBVIEW_OK:
            self._init_embedded_browser()
        else:
            lbl = ttk.Label(self.browser_frame, text="Navegador embedded nao disponivel.\n\nInstale: pip install tkinterweb\n\nOu clique no botao 'Abrir Navegador'\npara abrir no Chrome/Edge.", font=("Segoe UI", 10), foreground="gray", anchor=tk.CENTER, justify=tk.CENTER)
            lbl.pack(fill=tk.BOTH, expand=True)

    def _init_embedded_browser(self):
        try:
            from tkinterweb import HtmlFrame
            self._browser_widget = HtmlFrame(self.browser_frame)
            self._browser_widget.load_website(self.browser_url.get())
            self._browser_widget.pack(fill=tk.BOTH, expand=True)
        except:
            pass

    def _browser_go(self):
        url = self.browser_url.get()
        if self._browser_widget:
            try:
                self._browser_widget.load_website(url)
            except:
                webbrowser.open(url)
        else:
            webbrowser.open(url)

    def _open_browser(self):
        url = self.browser_url.get()
        webbrowser.open(url)

    # ================================================================
    # TAB: CONEXAO
    # ================================================================
    def _build_conn_tab(self):
        lf = ttk.LabelFrame(self.t_conn, text="Clientes WhatsApp", padding=6)
        lf.pack(fill=tk.BOTH, expand=True)

        # Canvas + scoll para multiplos clients
        canvas = tk.Canvas(lf, highlightthickness=0, bg=self.root.cget('bg'))
        scroll = ttk.Scrollbar(lf, orient=tk.VERTICAL, command=canvas.yview)
        self.conn_frame = ttk.Frame(canvas)
        self.conn_frame.bind("<Configure>", lambda e: canvas.configure(scrollregion=canvas.bbox("all")))
        canvas.create_window((0, 0), window=self.conn_frame, anchor="nw")
        canvas.configure(yscrollcommand=scroll.set)
        canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scroll.pack(side=tk.RIGHT, fill=tk.Y)
        self.conn_canvas = canvas

        self.conn_cards = {}

    def _rebuild_conn_cards(self, status_data):
        clients = status_data.get("clients", []) if isinstance(status_data, dict) else []
        if not clients:
            clients = [{"id": "principal", "connected": False, "ready": False, "qrcode": None, "stats": {"received": 0, "sent": 0}}]

        existing = set(self.conn_cards.keys())
        new_ids = set(c["id"] for c in clients)

        for cid in existing - new_ids:
            if cid in self.conn_cards:
                self.conn_cards[cid]["frame"].destroy()
                del self.conn_cards[cid]

        for c in clients:
            cid = c["id"]
            if cid not in self.conn_cards:
                self._create_conn_card(cid)
            self._update_conn_card(cid, c)

        self.conn_canvas.configure(scrollregion=self.conn_canvas.bbox("all"))

    def _create_conn_card(self, cid):
        f = ttk.LabelFrame(self.conn_frame, text=f"  {cid}  ", padding=6)
        f.pack(fill=tk.X, pady=3, padx=2)

        inner = ttk.Frame(f)
        inner.pack(fill=tk.X)

        # QR
        qf = ttk.Frame(inner)
        qf.pack(side=tk.LEFT, padx=(0, 10))
        qr_lbl = ttk.Label(qf, text="Aguardando QR...", font=("Segoe UI", 8))
        qr_lbl.pack()
        qr_c = tk.Canvas(qf, width=160, height=160, highlightthickness=0, bg="white")
        qr_c.pack()
        qr_c.create_text(80, 80, text="...", fill="gray")

        # Info
        inf = ttk.Frame(inner)
        inf.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        st_lbl = ttk.Label(inf, text="Aguardando...", font=("Segoe UI", 10, "bold"))
        st_lbl.pack(anchor=tk.W)
        st_det = ttk.Label(inf, text="", font=("Segoe UI", 8))
        st_det.pack(anchor=tk.W)
        actf = ttk.Frame(inf)
        actf.pack(anchor=tk.W, pady=(4, 0))
        b1 = ttk.Button(actf, text="Reconectar", width=14, command=lambda c=cid: self._reconnect_client(c))
        b1.pack(side=tk.LEFT, padx=(0, 4))
        b2 = ttk.Button(actf, text="Desativar Bot", width=14, command=lambda c=cid: self._toggle_client_bot(c))
        b2.pack(side=tk.LEFT)

        self.conn_cards[cid] = {"frame": f, "qr_label": qr_lbl, "qr_canvas": qr_c, "status": st_lbl, "details": st_det, "bot_btn": b2}

    def _update_conn_card(self, cid, data):
        card = self.conn_cards.get(cid)
        if not card: return
        conn = data.get("connected", False)
        ready = data.get("ready", False)
        st = data.get("stats", {})

        if conn and ready:
            card["status"].configure(text=f"\u2705 Conectado - {cid}", foreground="#2ecc71")
            card["details"].configure(text=f"Recebidas: {st.get('received', 0)} | Enviadas: {st.get('sent', 0)} | Humanos: {data.get('humanModeCount', 0)}")
            card["bot_btn"].configure(text="Desativar Bot")
            self._clear_qr_card(cid)
        elif conn and not ready:
            card["status"].configure(text=f"\u23f3 Conectando... - {cid}", foreground="#f39c12")
            card["details"].configure(text="WhatsApp Web ainda inicializando")
        else:
            card["status"].configure(text=f"\u274c Desconectado - {cid}", foreground="#e74c3c")
            card["details"].configure(text="Escaneie o QR Code abaixo")
            qr = data.get("qrcode")
            if qr: self._display_qr_card(cid, qr)
            else: card["qr_label"].configure(text="Aguardando QR Code...")

        if data.get("botEnabled") is False:
            card["bot_btn"].configure(text="Ativar Bot")

    def _display_qr_card(self, cid, qr_text):
        card = self.conn_cards.get(cid)
        if not card: return
        if QR_OK and qr_text:
            try:
                img = qrcode.make(qr_text).resize((160, 160), Image.NEAREST)
                self._qr_images[cid] = ImageTk.PhotoImage(img)
                card["qr_canvas"].delete("all")
                card["qr_canvas"].create_image(80, 80, image=self._qr_images[cid])
                card["qr_label"].configure(text="Escaneie com WhatsApp:")
                return
            except: pass
        card["qr_canvas"].delete("all")
        card["qr_canvas"].create_text(80, 80, text="QR", fill="gray", font=("Segoe UI", 9))
        card["qr_label"].configure(text="QR Code pronto")

    def _clear_qr_card(self, cid):
        card = self.conn_cards.get(cid)
        if not card: return
        card["qr_canvas"].delete("all")
        card["qr_canvas"].create_text(80, 80, text="\u2705", fill="green", font=("Segoe UI", 24))
        card["qr_label"].configure(text="Conectado")

    def _reconnect_client(self, cid):
        r = api_get(f"/status")
        if "error" in r: messagebox.showerror("Erro", "Servidor offline")
        else: messagebox.showinfo(cid, f"Status: {'Conectado' if r.get('connected') else 'Desconectado'}\n\nPara reconectar, reinicie o servidor Node.")

    def _toggle_client_bot(self, cid):
        r = api_post("/api/bot/toggle", {"clientId": cid})
        if r.get("success"):
            enabled = r.get("botEnabled", True)
            if cid in self.conn_cards:
                self.conn_cards[cid]["bot_btn"].configure(text="Desativar Bot" if enabled else "Ativar Bot")

    # ================================================================
    # TAB: ENVIAR
    # ================================================================
    def _build_send_tab(self):
        n = ttk.Notebook(self.t_send)
        n.pack(fill=tk.BOTH, expand=True)

        # --- Texto ---
        t1 = ttk.Frame(n, padding=6)
        n.add(t1, text="  Texto  ")
        self._build_send_text(t1)

        # --- Midia ---
        t2 = ttk.Frame(n, padding=6)
        n.add(t2, text="  Midia  ")
        self._build_send_media(t2)

        # --- Broadcast ---
        t3 = ttk.Frame(n, padding=6)
        n.add(t3, text="  Broadcast  ")
        self._build_send_broadcast(t3)

        # --- Enquete ---
        t4 = ttk.Frame(n, padding=6)
        n.add(t4, text="  Enquete  ")
        self._build_send_poll(t4)

    def _send_client_selector(self, parent, var):
        f = ttk.Frame(parent)
        f.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(f, text="Cliente:", font=("Segoe UI", 9)).pack(side=tk.LEFT, padx=(0, 4))
        cb = ttk.Combobox(f, textvariable=var, width=20, font=("Segoe UI", 9), state="readonly")
        cb.pack(side=tk.LEFT)
        self._client_combo = cb
        return cb

    def _refresh_client_combo(self):
        if hasattr(self, '_client_combo') and self._client_combo:
            r = api_get("/status")
            clients = [c["id"] for c in r.get("clients", [])] if isinstance(r, dict) else ["principal"]
            self._client_combo["values"] = clients
            if clients and not self._client_combo.get():
                self._client_combo.set(clients[0])

    def _build_send_text(self, parent):
        self._sc = tk.StringVar(value="principal")
        self._send_client_selector(parent, self._sc)
        ttk.Label(parent, text="Numero (com DDI):", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.sn = ttk.Entry(parent, font=("Segoe UI", 10))
        self.sn.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(parent, text="Mensagem:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.st = scrolledtext.ScrolledText(parent, height=8, font=("Segoe UI", 10))
        self.st.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        bf = ttk.Frame(parent)
        bf.pack(fill=tk.X)
        ttk.Button(bf, text="Enviar Mensagem", command=self._send_text).pack(side=tk.LEFT)
        self.sl = ttk.Label(bf, text="", font=("Segoe UI", 8))
        self.sl.pack(side=tk.LEFT, padx=8)

    def _build_send_media(self, parent):
        self._smc = tk.StringVar(value="principal")
        self._send_client_selector(parent, self._smc)
        ttk.Label(parent, text="Numero (com DDI):", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.mn = ttk.Entry(parent, font=("Segoe UI", 10))
        self.mn.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(parent, text="URL (ou deixe vazio para arquivo local):", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.mu = ttk.Entry(parent, font=("Segoe UI", 10))
        self.mu.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(parent, text="Legenda:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.mc = ttk.Entry(parent, font=("Segoe UI", 10))
        self.mc.pack(fill=tk.X, pady=(0, 6))
        bf = ttk.Frame(parent)
        bf.pack(fill=tk.X)
        ttk.Button(bf, text="Enviar por URL", command=self._send_media_url).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf, text="Selecionar Arquivo", command=self._send_media_file).pack(side=tk.LEFT)
        self.ml = ttk.Label(bf, text="", font=("Segoe UI", 8))
        self.ml.pack(side=tk.LEFT, padx=8)

    def _build_send_broadcast(self, parent):
        self._sbc = tk.StringVar(value="principal")
        self._send_client_selector(parent, self._sbc)
        ttk.Label(parent, text="Numeros (um por linha):", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.bn = scrolledtext.ScrolledText(parent, height=4, font=("Segoe UI", 10))
        self.bn.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(parent, text="Mensagem:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.bt = scrolledtext.ScrolledText(parent, height=6, font=("Segoe UI", 10))
        self.bt.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        bf = ttk.Frame(parent)
        bf.pack(fill=tk.X)
        ttk.Button(bf, text="Enviar para Todos", command=self._send_broadcast).pack(side=tk.LEFT)
        self.bl = ttk.Label(bf, text="", font=("Segoe UI", 8))
        self.bl.pack(side=tk.LEFT, padx=8)

    def _build_send_poll(self, parent):
        self._spc = tk.StringVar(value="principal")
        self._send_client_selector(parent, self._spc)
        ttk.Label(parent, text="Numero (com DDI):", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.pn = ttk.Entry(parent, font=("Segoe UI", 10))
        self.pn.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(parent, text="Pergunta:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.pq = ttk.Entry(parent, font=("Segoe UI", 10))
        self.pq.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(parent, text="Opcoes (1 por linha, min 2):", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.po = scrolledtext.ScrolledText(parent, height=5, font=("Segoe UI", 10))
        self.po.pack(fill=tk.BOTH, expand=True, pady=(0, 6))
        bf = ttk.Frame(parent)
        bf.pack(fill=tk.X)
        ttk.Button(bf, text="Criar Enquete", command=self._send_poll).pack(side=tk.LEFT)
        self.pl = ttk.Label(bf, text="", font=("Segoe UI", 8))
        self.pl.pack(side=tk.LEFT, padx=8)

    def _send_text(self):
        num = self.sn.get().strip()
        txt = self.st.get("1.0", tk.END).strip()
        cid = self._sc.get()
        if not num or not txt: self.sl.configure(text="Preencha numero e mensagem!", foreground="red"); return
        self.sl.configure(text="Enviando...")
        def task():
            r = api_post("/api/send/text", {"number": num, "message": txt, "clientId": cid})
            self.root.after(0, lambda: self._set_label(self.sl, r))
        threading.Thread(target=task, daemon=True).start()

    def _send_media_url(self):
        num = self.mn.get().strip(); url = self.mu.get().strip(); cap = self.mc.get().strip(); cid = self._smc.get()
        if not num or not url: self.ml.configure(text="Preencha numero e URL!", foreground="red"); return
        self.ml.configure(text="Enviando...")
        def task():
            r = api_post("/api/send/media", {"number": num, "url": url, "caption": cap, "clientId": cid})
            self.root.after(0, lambda: self._set_label(self.ml, r))
        threading.Thread(target=task, daemon=True).start()

    def _send_media_file(self):
        fp = filedialog.askopenfilename(title="Selecionar arquivo")
        if not fp: return
        num = self.mn.get().strip(); cap = self.mc.get().strip(); cid = self._smc.get()
        if not num: self.ml.configure(text="Preencha o numero!", foreground="red"); return
        self.ml.configure(text="Fazendo upload...")
        def task():
            import base64
            with open(fp, "rb") as f:
                encoded = base64.b64encode(f.read()).decode()
            fileName = os.path.basename(fp)
            upload = api_post("/api/upload", {"fileName": fileName, "data": encoded})
            if upload.get("success"):
                r = api_post("/api/send/media", {"number": num, "fileName": fileName, "caption": cap, "clientId": cid})
                self.root.after(0, lambda: self._set_label(self.ml, r))
            else:
                self.root.after(0, lambda: self.ml.configure(text=f"Upload falhou: {upload.get('error', '?')}", foreground="red"))
        threading.Thread(target=task, daemon=True).start()

    def _send_broadcast(self):
        nums = [n.strip() for n in self.bn.get("1.0", tk.END).strip().split("\n") if n.strip()]
        txt = self.bt.get("1.0", tk.END).strip(); cid = self._sbc.get()
        if not nums or not txt: self.bl.configure(text="Preencha numeros e texto!", foreground="red"); return
        self.bl.configure(text=f"Enviando para {len(nums)}...")
        def task():
            r = api_post("/api/broadcast", {"numbers": nums, "message": txt, "clientId": cid})
            self.root.after(0, lambda: self._bc_result(r))
        threading.Thread(target=task, daemon=True).start()

    def _send_poll(self):
        num = self.pn.get().strip(); q = self.pq.get().strip()
        opts = [o.strip() for o in self.po.get("1.0", tk.END).strip().split("\n") if o.strip()]
        cid = self._spc.get()
        if not num or not q or len(opts) < 2: self.pl.configure(text="Preencha numero, pergunta e 2+ opcoes!", foreground="red"); return
        self.pl.configure(text="Criando...")
        def task():
            r = api_post("/api/send/poll", {"number": num, "question": q, "options": opts, "clientId": cid})
            self.root.after(0, lambda: self._set_label(self.pl, r))
        threading.Thread(target=task, daemon=True).start()

    def _set_label(self, lbl, r):
        if r.get("success"): lbl.configure(text="OK!", foreground="green")
        else: lbl.configure(text=f"Erro: {r.get('error', '?')[:50]}", foreground="red")

    def _bc_result(self, r):
        if r.get("success"):
            results = r.get("results", [])
            ok = sum(1 for x in results if x.get("success"))
            fail = sum(1 for x in results if not x.get("success"))
            self.bl.configure(text=f"OK: {ok} | Falhas: {fail}", foreground="green" if fail == 0 else "orange")
        else: self.bl.configure(text=f"Erro: {r.get('error', '?')[:50]}", foreground="red")

    # ================================================================
    # TAB: MENSAGENS
    # ================================================================
    def _build_msg_tab(self):
        self.msg_nb = ttk.Notebook(self.t_msg)
        self.msg_nb.pack(fill=tk.BOTH, expand=True)

        # Tab: Todas as mensagens
        self.msg_all_frame = ttk.Frame(self.msg_nb, padding=4)
        self.msg_nb.add(self.msg_all_frame, text="  Todas  ")

        top = ttk.Frame(self.msg_all_frame)
        top.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(top, text="Filtrar numero:", font=("Segoe UI", 9)).pack(side=tk.LEFT, padx=(0, 4))
        self.mf = ttk.Entry(top, width=18, font=("Segoe UI", 9))
        self.mf.pack(side=tk.LEFT, padx=(0, 4))
        ttk.Label(top, text="Cliente:", font=("Segoe UI", 9)).pack(side=tk.LEFT, padx=(0, 4))
        self.mc_var = tk.StringVar()
        self.mc_combo = ttk.Combobox(top, textvariable=self.mc_var, width=14, font=("Segoe UI", 9), state="readonly")
        self.mc_combo.pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(top, text="Buscar", command=self._refresh_msgs).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(top, text="Limpar", command=self._clear_msgs).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(top, text="Atualizar", command=self._refresh_msgs).pack(side=tk.LEFT)

        cols = ("id", "client", "from", "body", "media", "time")
        self.mt = ttk.Treeview(self.msg_all_frame, columns=cols, show="headings", height=16)
        for c, w in zip(cols, [50, 70, 120, 300, 50, 130]):
            self.mt.heading(c, text=c.capitalize())
            self.mt.column(c, width=w, minwidth=40)
        sb = ttk.Scrollbar(self.msg_all_frame, orient=tk.VERTICAL, command=self.mt.yview)
        self.mt.configure(yscrollcommand=sb.set)
        self.mt.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        sb.pack(side=tk.RIGHT, fill=tk.Y)

        # Tab: Conversas
        self.msg_conv_frame = ttk.Frame(self.msg_nb, padding=4)
        self.msg_nb.add(self.msg_conv_frame, text="  Conversas  ")

        conv_top = ttk.Frame(self.msg_conv_frame)
        conv_top.pack(fill=tk.X, pady=(0, 4))
        ttk.Button(conv_top, text="Atualizar Conversas", command=self._refresh_convs).pack(side=tk.LEFT)

        self.conv_list = tk.Listbox(self.msg_conv_frame, font=("Segoe UI", 10), height=10)
        self.conv_list.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        self.conv_list.bind("<<ListboxSelect>>", self._on_conv_select)

        self.conv_detail = scrolledtext.ScrolledText(self.msg_conv_frame, height=10, font=("Consolas", 10), wrap=tk.WORD, state=tk.DISABLED)
        self.conv_detail.pack(fill=tk.BOTH, expand=True)

    # ================================================================
    # TAB: CONFIGURACAO
    # ================================================================
    def _build_config_tab(self):
        n = ttk.Notebook(self.t_config)
        n.pack(fill=tk.BOTH, expand=True)

        # --- Modo ---
        t0 = ttk.Frame(n, padding=6)
        n.add(t0, text="  Modo  ")
        self._build_mode_tab(t0)

        # --- Clientes ---
        t1 = ttk.Frame(n, padding=6)
        n.add(t1, text="  Clientes  ")
        self._build_clients_tab(t1)

        # --- Blacklist ---
        t2 = ttk.Frame(n, padding=6)
        n.add(t2, text="  Blacklist  ")
        self._build_blacklist_tab(t2)

        # --- Webhook ---
        t3 = ttk.Frame(n, padding=6)
        n.add(t3, text="  Webhook  ")
        self._build_webhook_tab(t3)

        # --- Humanos ---
        t4 = ttk.Frame(n, padding=6)
        n.add(t4, text="  Modo Humano  ")
        self._build_human_tab(t4)

        # --- IA ---
        t5 = ttk.Frame(n, padding=6)
        n.add(t5, text="  IA  ")
        self._build_ai_tab(t5)

    def _build_ai_tab(self, parent):
        lf = ttk.LabelFrame(parent, text="Provedor de IA")
        lf.pack(fill=tk.X, pady=(0, 8))
        self.ai_provider = tk.StringVar(value="openai")
        ttk.Radiobutton(lf, text="OpenAI (ChatGPT)", variable=self.ai_provider, value="openai", command=self._toggle_ai_panels).pack(anchor=tk.W, pady=2)
        ttk.Radiobutton(lf, text="Ollama (local - gratuita)", variable=self.ai_provider, value="ollama", command=self._toggle_ai_panels).pack(anchor=tk.W, pady=2)

        self.ai_openai_frame = ttk.LabelFrame(parent, text="OpenAI")
        self.ai_openai_frame.pack(fill=tk.X, pady=(0, 8))
        ttk.Label(self.ai_openai_frame, text="API Key:").pack(anchor=tk.W)
        self.ai_openai_key = ttk.Entry(self.ai_openai_frame, width=50, font=("Consolas", 9), show="*")
        self.ai_openai_key.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(self.ai_openai_frame, text="Modelo:").pack(anchor=tk.W)
        self.ai_openai_model = ttk.Combobox(self.ai_openai_frame, values=["gpt-4o-mini", "gpt-4o", "gpt-4", "gpt-3.5-turbo"], font=("Segoe UI", 9), state="readonly")
        self.ai_openai_model.pack(fill=tk.X, pady=(0, 2))
        self.ai_openai_model.set("gpt-4o-mini")

        self.ai_ollama_frame = ttk.LabelFrame(parent, text="Ollama")
        self.ai_ollama_frame.pack(fill=tk.X, pady=(0, 8))
        ttk.Label(self.ai_ollama_frame, text="URL do servidor:").pack(anchor=tk.W)
        self.ai_ollama_url = ttk.Entry(self.ai_ollama_frame, width=50, font=("Consolas", 9))
        self.ai_ollama_url.pack(fill=tk.X, pady=(0, 4))
        self.ai_ollama_url.insert(0, "http://localhost:11434")
        ttk.Label(self.ai_ollama_frame, text="Modelo:").pack(anchor=tk.W)
        self.ai_ollama_model = ttk.Entry(self.ai_ollama_frame, width=30, font=("Segoe UI", 9))
        self.ai_ollama_model.pack(fill=tk.X, pady=(0, 2))
        self.ai_ollama_model.insert(0, "llama3")
        ttk.Label(self.ai_ollama_frame, text="Baixe modelos em ollama.com/library", font=("Segoe UI", 8), foreground="gray").pack(anchor=tk.W)

        bf = ttk.Frame(parent)
        bf.pack(fill=tk.X, pady=4)
        ttk.Button(bf, text="Salvar Configuracao de IA", command=self._save_ai_config).pack(side=tk.LEFT, padx=(0, 8))
        ttk.Button(bf, text="Testar Conexao", command=self._test_ai).pack(side=tk.LEFT)

        self.ai_test_result = scrolledtext.ScrolledText(parent, height=4, font=("Consolas", 10), wrap=tk.WORD)
        self.ai_test_result.pack(fill=tk.X, pady=(4, 0))

    def _toggle_ai_panels(self):
        v = self.ai_provider.get()
        self.ai_openai_frame.pack_forget()
        self.ai_ollama_frame.pack_forget()
        if v == "openai":
            self.ai_openai_frame.pack(fill=tk.X, pady=(0, 8))
        else:
            self.ai_ollama_frame.pack(fill=tk.X, pady=(0, 8))

    def _save_ai_config(self):
        body = {
            "aiProvider": self.ai_provider.get(),
            "openaiModel": self.ai_openai_model.get(),
            "ollamaUrl": self.ai_ollama_url.get(),
            "ollamaModel": self.ai_ollama_model.get(),
        }
        key = self.ai_openai_key.get().strip()
        if key and key != "(configurada)":
            body["openaiKey"] = key
        r = api_put("/api/config/ai", body)
        if r.get("success"):
            messagebox.showinfo("IA", "Configuracao de IA salva!")
        else:
            messagebox.showerror("Erro", r.get("error", "Falha ao salvar"))

    def _test_ai(self):
        self.ai_test_result.delete("1.0", tk.END)
        self.ai_test_result.insert("1.0", "Testando...")
        def task():
            r = api_post("/api/send/ai", {"number": "00000000000", "prompt": "Ola! Como voce esta? Responda em portugues com 1 frase.", "clientId": "_test_"})
            self.root.after(0, lambda: self._show_ai_test(r))
        threading.Thread(target=task, daemon=True).start()

    def _show_ai_test(self, r):
        self.ai_test_result.delete("1.0", tk.END)
        if r.get("response"):
            self.ai_test_result.insert("1.0", f"Resposta:\n{r['response']}")
        elif r.get("error"):
            self.ai_test_result.insert("1.0", f"Erro: {r['error']}")
        else:
            self.ai_test_result.insert("1.0", "IA nao disponivel. Configure OpenAI ou Ollama e tente novamente.")

    def _build_mode_tab(self, parent):
        lf = ttk.LabelFrame(parent, text="Modo de Operacao")
        lf.pack(fill=tk.X, pady=(0, 8))
        self.mode_var = tk.StringVar(value="single")
        for m, d in [("single", "1 numero, bot direto"), ("central", "1 numero central + redirecionamentos"), ("multi", "Multiplos numeros independentes")]:
            ttk.Radiobutton(lf, text=f"{m.upper()} - {d}", value=m, variable=self.mode_var).pack(anchor=tk.W, pady=2)

        lf2 = ttk.LabelFrame(parent, text="Controles Globais")
        lf2.pack(fill=tk.X, pady=(0, 8))
        f1 = ttk.Frame(lf2); f1.pack(fill=tk.X, pady=2)
        self.gb_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(f1, text="Bot Ativo", variable=self.gb_var, command=self._toggle_global_bot).pack(side=tk.LEFT, padx=(0, 20))
        self.gv_var = tk.BooleanVar(value=False)
        ttk.Checkbutton(f1, text="Modo Ferias (Ausente)", variable=self.gv_var, command=self._toggle_vacation).pack(side=tk.LEFT)
        ttk.Label(f1, text="  Mensagem:").pack(side=tk.LEFT, padx=(4, 0))
        self.vm_entry = ttk.Entry(f1, width=40, font=("Segoe UI", 9))
        self.vm_entry.pack(side=tk.LEFT, padx=4)
        self.vm_entry.insert(0, "Estamos ausentes. Retornaremos em breve.")
        self.vm_entry.bind("<KeyRelease>", lambda e: self._save_vacation_msg())

        lf3 = ttk.LabelFrame(parent, text="Configuracoes")
        lf3.pack(fill=tk.X)
        ttk.Label(lf3, text="Nome do Bot:").grid(row=0, column=0, sticky=tk.W, padx=4, pady=2)
        self.bn_entry = ttk.Entry(lf3, width=40, font=("Segoe UI", 10))
        self.bn_entry.grid(row=0, column=1, padx=4, pady=2)
        ttk.Label(lf3, text="Controller Number:").grid(row=1, column=0, sticky=tk.W, padx=4, pady=2)
        self.cn_entry = ttk.Entry(lf3, width=40, font=("Segoe UI", 10))
        self.cn_entry.grid(row=1, column=1, padx=4, pady=2)
        ttk.Label(lf3, text="Reset Humano (h):").grid(row=2, column=0, sticky=tk.W, padx=4, pady=2)
        self.hr_entry = ttk.Entry(lf3, width=10, font=("Segoe UI", 10))
        self.hr_entry.grid(row=2, column=1, sticky=tk.W, padx=4, pady=2)
        ttk.Label(lf3, text="Chromium Path:").grid(row=3, column=0, sticky=tk.W, padx=4, pady=2)
        self.cp_entry = ttk.Entry(lf3, width=40, font=("Segoe UI", 9))
        self.cp_entry.grid(row=3, column=1, padx=4, pady=2)
        ttk.Button(lf3, text="Salvar Config e Reiniciar Servidor", command=self._save_config).grid(row=4, column=0, columnspan=2, pady=8)

    def _build_clients_tab(self, parent):
        left = ttk.Frame(parent)
        left.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        right = ttk.Frame(parent)
        right.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(6, 0))

        ttk.Label(left, text="Clientes configurados:", font=("Segoe UI", 9, "bold")).pack(anchor=tk.W)
        self.cl_list = tk.Listbox(left, font=("Consolas", 10), height=14)
        sb = ttk.Scrollbar(left, orient=tk.VERTICAL, command=self.cl_list.yview)
        self.cl_list.configure(yscrollcommand=sb.set)
        self.cl_list.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        sb.pack(side=tk.RIGHT, fill=tk.Y)
        self.cl_list.bind("<<ListboxSelect>>", self._on_client_select)

        ttk.Label(right, text="Editor Rapido (config.json):", font=("Segoe UI", 9, "bold")).pack(anchor=tk.W)
        self.cl_edit = scrolledtext.ScrolledText(right, height=18, font=("Consolas", 9), wrap=tk.WORD)
        self.cl_edit.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        bf = ttk.Frame(right)
        bf.pack(fill=tk.X)
        ttk.Button(bf, text="Carregar Config", command=self._load_config_gui).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf, text="Salvar Config.json", command=self._save_config_file).pack(side=tk.LEFT)

    def _build_blacklist_tab(self, parent):
        ttk.Label(parent, text="Selecione o cliente:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        f1 = ttk.Frame(parent); f1.pack(fill=tk.X)
        self.blc_var = tk.StringVar(value="principal")
        self.blc_cb = ttk.Combobox(f1, textvariable=self.blc_var, width=20, font=("Segoe UI", 9), state="readonly")
        self.blc_cb.pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(f1, text="Carregar Blacklist", command=self._refresh_bl).pack(side=tk.LEFT)

        self.bl_list = tk.Listbox(parent, font=("Consolas", 10), height=10)
        self.bl_list.pack(fill=tk.BOTH, expand=True, pady=4)

        f2 = ttk.Frame(parent); f2.pack(fill=tk.X)
        ttk.Label(f2, text="Numero:").pack(side=tk.LEFT, padx=(0, 4))
        self.bl_entry = ttk.Entry(f2, width=20, font=("Segoe UI", 9))
        self.bl_entry.pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(f2, text="Bloquear", command=self._block_num).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(f2, text="Desbloquear", command=self._unblock_num).pack(side=tk.LEFT)

    def _build_webhook_tab(self, parent):
        ttk.Label(parent, text="URL do Webhook:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.wh_var = tk.StringVar()
        ttk.Entry(parent, textvariable=self.wh_var, font=("Segoe UI", 10)).pack(fill=tk.X, pady=(0, 4))
        ttk.Label(parent, text="Recebe POST com dados da mensagem quando chegar algo.", font=("Segoe UI", 8), foreground="gray").pack(anchor=tk.W)
        f = ttk.Frame(parent); f.pack(fill=tk.X, pady=4)
        ttk.Button(f, text="Definir", command=self._set_wh).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(f, text="Remover", command=self._del_wh).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(f, text="Verificar", command=self._check_wh).pack(side=tk.LEFT)

    def _build_human_tab(self, parent):
        ttk.Label(parent, text="Selecione o cliente:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        f1 = ttk.Frame(parent); f1.pack(fill=tk.X)
        self.hc_var = tk.StringVar(value="principal")
        self.hc_cb = ttk.Combobox(f1, textvariable=self.hc_var, width=20, font=("Segoe UI", 9), state="readonly")
        self.hc_cb.pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(f1, text="Carregar", command=self._refresh_human).pack(side=tk.LEFT)

        self.hm_list = tk.Listbox(parent, font=("Consolas", 10), height=8)
        self.hm_list.pack(fill=tk.BOTH, expand=True, pady=4)

        f2 = ttk.Frame(parent); f2.pack(fill=tk.X)
        ttk.Label(f2, text="Numero:").pack(side=tk.LEFT, padx=(0, 4))
        self.hm_entry = ttk.Entry(f2, width=20, font=("Segoe UI", 9))
        self.hm_entry.pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(f2, text="Ativar Humano", command=lambda: self._hm_toggle("enable")).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(f2, text="Desativar Humano", command=lambda: self._hm_toggle("disable")).pack(side=tk.LEFT)

    # ================================================================
    # TAB: FERRAMENTAS
    # ================================================================
    def _build_tools_tab(self):
        n = ttk.Notebook(self.t_tools)
        n.pack(fill=tk.BOTH, expand=True)

        # Templates
        t1 = ttk.Frame(n, padding=6)
        n.add(t1, text="  Templates  ")
        self._build_templates_tab(t1)

        # Schedules
        t2 = ttk.Frame(n, padding=6)
        n.add(t2, text="  Agendamentos  ")
        self._build_schedules_tab(t2)

        # Bot Responses
        t3 = ttk.Frame(n, padding=6)
        n.add(t3, text="  Intencoes  ")
        self._build_responses_tab(t3)

        # Log
        t4 = ttk.Frame(n, padding=6)
        n.add(t4, text="  Log  ")
        self._build_log_tab(t4)

        # Backup
        t5 = ttk.Frame(n, padding=6)
        n.add(t5, text="  Backup  ")
        self._build_backup_tab(t5)

    def _build_log_tab(self, parent):
        top = ttk.Frame(parent)
        top.pack(fill=tk.X, pady=(0, 4))
        ttk.Button(top, text="Atualizar", command=self._refresh_log).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(top, text="Limpar Log", command=self._clear_log).pack(side=tk.LEFT)
        self.log_status = ttk.Label(top, text="", font=("Segoe UI", 8), foreground="gray")
        self.log_status.pack(side=tk.LEFT, padx=8)
        self.log_text = scrolledtext.ScrolledText(parent, font=("Consolas", 9), wrap=tk.WORD, state=tk.DISABLED)
        self.log_text.pack(fill=tk.BOTH, expand=True)

    def _refresh_log(self):
        r = api_get("/api/log?lines=200")
        if "error" in r:
            self.log_status.configure(text="Erro ao carregar log")
            return
        self.log_text.configure(state=tk.NORMAL)
        self.log_text.delete("1.0", tk.END)
        for line in r.get("lines", []):
            tag = "info"
            if "WARN" in line: tag = "warn"
            elif "ERROR" in line: tag = "error"
            elif "DEBUG" in line: tag = "debug"
            self.log_text.insert(tk.END, line + "\n", tag)
        self.log_text.configure(state=tk.DISABLED)
        self.log_text.see(tk.END)
        self.log_status.configure(text=f"{r.get('total', 0)} linhas")
        self.log_text.tag_config("warn", foreground="#f59e0b")
        self.log_text.tag_config("error", foreground="#ef4444")
        self.log_text.tag_config("debug", foreground="#94a3b8")

    def _clear_log(self):
        if messagebox.askyesno("Limpar Log", "Limpar arquivo de log?"):
            r = api_delete("/api/log")
            if r.get("success"):
                self._refresh_log()

    def _build_backup_tab(self, parent):
        ttk.Label(parent, text="Backup e Restauracao do Banco de Dados", font=("Segoe UI", 10, "bold")).pack(anchor=tk.W, pady=(0, 12))
        bf = ttk.Frame(parent)
        bf.pack(fill=tk.X, pady=4)
        ttk.Button(bf, text="Baixar Backup (.db)", command=self._backup_db).pack(side=tk.LEFT, padx=(0, 8))
        ttk.Button(bf, text="Restaurar Backup", command=self._restore_db).pack(side=tk.LEFT)
        self.backup_status = ttk.Label(parent, text="", font=("Segoe UI", 9))
        self.backup_status.pack(anchor=tk.W, pady=4)
        ttk.Label(parent, text="O backup baixa o arquivo SQLite completo. Para restaurar, selecione um arquivo .db.", font=("Segoe UI", 8), foreground="gray").pack(anchor=tk.W)

    def _backup_db(self):
        self.backup_status.configure(text="Gerando backup...")
        def task():
            r = api_get("/api/backup")
            if r.get("data"):
                import base64
                data = base64.b64decode(r["data"])
                fp = filedialog.asksaveasfilename(
                    defaultextension=".db",
                    filetypes=[("SQLite DB", "*.db"), ("Todos", "*.*")],
                    initialfile=f"zap-backup-{datetime.now().strftime('%Y-%m-%d')}.db"
                )
                if fp:
                    with open(fp, "wb") as f:
                        f.write(data)
                    self.root.after(0, lambda: self.backup_status.configure(text=f"Backup salvo: {os.path.basename(fp)} ({len(data)/1024:.1f} KB)"))
                else:
                    self.root.after(0, lambda: self.backup_status.configure(text="Cancelado"))
            else:
                self.root.after(0, lambda: self.backup_status.configure(text="Erro ao gerar backup"))
        threading.Thread(target=task, daemon=True).start()

    def _restore_db(self):
        fp = filedialog.askopenfilename(title="Selecionar arquivo de backup", filetypes=[("SQLite DB", "*.db"), ("Todos", "*.*")])
        if not fp: return
        if not messagebox.askyesno("Restaurar", "Isso substituira o banco atual por este backup. Continuar?"):
            return
        self.backup_status.configure(text="Restaurando...")
        def task():
            import base64
            with open(fp, "rb") as f:
                data = base64.b64encode(f.read()).decode()
            r = api_post("/api/backup/restore", {"data": data})
            if r.get("success"):
                self.root.after(0, lambda: self.backup_status.configure(text="Banco restaurado com sucesso!"))
                messagebox.showinfo("Restaurado", "Banco de dados restaurado. O servidor recarregou automaticamente.")
            else:
                self.root.after(0, lambda: self.backup_status.configure(text=f"Erro: {r.get('error', 'desconhecido')}"))
        threading.Thread(target=task, daemon=True).start()

    def _build_templates_tab(self, parent):
        left = ttk.Frame(parent)
        left.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        right = ttk.Frame(parent)
        right.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(6, 0))

        ttk.Label(left, text="Templates salvos:", font=("Segoe UI", 9, "bold")).pack(anchor=tk.W)
        self.tp_list = tk.Listbox(left, font=("Segoe UI", 10), height=10)
        self.tp_list.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        self.tp_list.bind("<<ListboxSelect>>", self._on_template_select)
        ttk.Button(left, text="Atualizar", command=self._refresh_templates).pack()

        ttk.Label(right, text="Nome:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.tp_name = ttk.Entry(right, font=("Segoe UI", 9))
        self.tp_name.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(right, text="Conteudo:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.tp_content = scrolledtext.ScrolledText(right, height=8, font=("Segoe UI", 10))
        self.tp_content.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        bf = ttk.Frame(right)
        bf.pack(fill=tk.X)
        ttk.Button(bf, text="Salvar Template", command=self._save_template).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf, text="Usar no Envio", command=self._use_template).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf, text="Deletar", command=self._del_template).pack(side=tk.LEFT)

    def _build_schedules_tab(self, parent):
        left = ttk.Frame(parent)
        left.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        right = ttk.Frame(parent)
        right.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(6, 0))

        ttk.Label(left, text="Agendamentos:", font=("Segoe UI", 9, "bold")).pack(anchor=tk.W)
        self.sch_list = tk.Listbox(left, font=("Consolas", 10), height=10)
        self.sch_list.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        ttk.Button(left, text="Atualizar", command=self._refresh_schedules).pack()

        lf = ttk.LabelFrame(right, text="Novo Agendamento")
        lf.pack(fill=tk.BOTH, expand=True)
        ttk.Label(lf, text="Nome:").pack(anchor=tk.W)
        self.sch_name = ttk.Entry(lf, font=("Segoe UI", 9))
        self.sch_name.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(lf, text="Telefone:").pack(anchor=tk.W)
        self.sch_phone = ttk.Entry(lf, font=("Segoe UI", 9))
        self.sch_phone.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(lf, text="Horario (HH:MM):").pack(anchor=tk.W)
        self.sch_time = ttk.Entry(lf, font=("Segoe UI", 9))
        self.sch_time.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(lf, text="Mensagem:").pack(anchor=tk.W)
        self.sch_msg = scrolledtext.ScrolledText(lf, height=4, font=("Segoe UI", 10))
        self.sch_msg.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        ttk.Label(lf, text="Cliente:").pack(anchor=tk.W)
        self.sch_client = ttk.Combobox(lf, values=["principal"], font=("Segoe UI", 9), state="readonly")
        self.sch_client.pack(fill=tk.X, pady=(0, 4))
        bf = ttk.Frame(lf)
        bf.pack(fill=tk.X)
        ttk.Button(bf, text="Agendar", command=self._add_schedule).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf, text="Desativar Selecionado", command=self._toggle_schedule).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf, text="Deletar", command=self._del_schedule).pack(side=tk.LEFT)

    def _build_responses_tab(self, parent):
        left = ttk.Frame(parent)
        left.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        right = ttk.Frame(parent)
        right.pack(side=tk.RIGHT, fill=tk.BOTH, expand=True, padx=(6, 0))

        ttk.Label(left, text="Intencoes configuradas:", font=("Segoe UI", 9, "bold")).pack(anchor=tk.W)
        self.resp_list = tk.Listbox(left, font=("Segoe UI", 10), height=12)
        self.resp_list.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        self.resp_list.bind("<<ListboxSelect>>", self._on_resp_select)
        bf1 = ttk.Frame(left)
        bf1.pack(fill=tk.X)
        ttk.Button(bf1, text="Atualizar", command=self._load_responses).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf1, text="Nova Intencao", command=self._new_intent).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf1, text="Remover", command=self._remove_intent).pack(side=tk.LEFT)

        ttk.Label(right, text="Palavras-chave (separadas por virgula):", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.int_kw = ttk.Entry(right, font=("Segoe UI", 10))
        self.int_kw.pack(fill=tk.X, pady=(0, 4))
        ttk.Label(right, text="Resposta:", font=("Segoe UI", 9)).pack(anchor=tk.W)
        self.int_resp = scrolledtext.ScrolledText(right, height=8, font=("Segoe UI", 10))
        self.int_resp.pack(fill=tk.BOTH, expand=True, pady=(0, 4))
        bf2 = ttk.Frame(right)
        bf2.pack(fill=tk.X)
        ttk.Button(bf2, text="Salvar Intencao", command=self._save_intent).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf2, text="Salvar Todas", command=self._save_all_intents).pack(side=tk.LEFT)
        ttk.Label(right, text="As intencoes sao salvas globalmente no config.json", font=("Segoe UI", 8), foreground="gray").pack(anchor=tk.W, pady=(4, 0))

    def _load_responses(self):
        data = api_get("/api/config")
        if "error" in data: return
        self._intents_data = data.get("intents", [])
        self.resp_list.delete(0, tk.END)
        for i, intent in enumerate(self._intents_data):
            label = ", ".join(intent.get("patterns", ["?"]))
            self.resp_list.insert(tk.END, f"#{i+1} {label}")
        self.int_kw.delete(0, tk.END)
        self.int_resp.delete("1.0", tk.END)

    def _on_resp_select(self, e):
        sel = self.resp_list.curselection()
        if not sel: return
        idx = sel[0]
        if idx < len(self._intents_data):
            intent = self._intents_data[idx]
            self.int_kw.delete(0, tk.END)
            self.int_kw.insert(0, ", ".join(intent.get("patterns", [])))
            self.int_resp.delete("1.0", tk.END)
            self.int_resp.insert("1.0", intent.get("response", ""))
            self._selected_intent_idx = idx
        else:
            self._selected_intent_idx = None

    def _new_intent(self):
        self._intents_data.append({"patterns": [], "response": "", "redirect": False})
        self.resp_list.insert(tk.END, f"#{len(self._intents_data)} (novo)")
        self.resp_list.selection_clear(0, tk.END)
        self.resp_list.selection_set(tk.END)
        self._on_resp_select(None)

    def _remove_intent(self):
        sel = self.resp_list.curselection()
        if not sel: return
        idx = sel[0]
        if idx < len(self._intents_data):
            label = ", ".join(self._intents_data[idx].get("patterns", ["?"]))
            if messagebox.askyesno("Remover", f"Remover intencao '{label}'?"):
                self._intents_data.pop(idx)
                self._load_responses()

    def _save_intent(self):
        kw = self.int_kw.get().strip()
        resp = self.int_resp.get("1.0", tk.END).strip()
        if not kw or not resp:
            messagebox.showerror("Erro", "Preencha palavras-chave e resposta")
            return
        patterns = [p.strip() for p in kw.split(",") if p.strip()]
        if hasattr(self, '_selected_intent_idx') and self._selected_intent_idx is not None and self._selected_intent_idx < len(self._intents_data):
            self._intents_data[self._selected_intent_idx] = {"patterns": patterns, "response": resp, "redirect": False}
        else:
            self._intents_data.append({"patterns": patterns, "response": resp, "redirect": False})
        self._save_all_intents(silent=True)

    def _save_all_intents(self, silent=False):
        data = api_get("/api/config")
        if "error" in data: return
        data["intents"] = self._intents_data
        r = api_put("/api/config", data)
        if r.get("success"):
            if not silent: messagebox.showinfo("Intencoes", f"{len(self._intents_data)} intencoes salvas!")
            self._load_responses()

    # ================================================================
    # TAB: ESTATISTICAS
    # ================================================================
    def _build_stats_tab(self):
        self.stats_text = scrolledtext.ScrolledText(self.t_stats, font=("Consolas", 11), wrap=tk.WORD, state=tk.DISABLED)
        self.stats_text.pack(fill=tk.BOTH, expand=True, pady=(0, 6))
        bf = ttk.Frame(self.t_stats)
        bf.pack(fill=tk.X)
        ttk.Button(bf, text="Atualizar", command=self._refresh_stats).pack(side=tk.LEFT, padx=(0, 4))
        ttk.Button(bf, text="Abrir Log", command=self._open_log).pack(side=tk.LEFT)

    # ================================================================
    # ACOES: CONFIG
    # ================================================================
    def _load_config_gui(self):
        data = api_get("/api/config")
        if "error" in data:
            messagebox.showerror("Erro", data["error"])
            return
        self._config_data = data
        txt = json.dumps(data, indent=2, ensure_ascii=False)
        self.cl_edit.delete("1.0", tk.END)
        self.cl_edit.insert("1.0", txt)
        # Update mode tab
        self.mode_var.set(data.get("mode", "single"))
        self.gb_var.set(data.get("botEnabled", True))
        self.gv_var.set(data.get("vacationMode", False))
        self.vm_entry.delete(0, tk.END)
        self.vm_entry.insert(0, data.get("vacationMessage", ""))
        self.bn_entry.delete(0, tk.END)
        self.bn_entry.insert(0, data.get("botName", ""))
        self.cn_entry.delete(0, tk.END)
        self.cn_entry.insert(0, data.get("controllerNumber", ""))
        self.hr_entry.delete(0, tk.END)
        self.hr_entry.insert(0, str(data.get("humanResetHours", 24)))
        self.cp_entry.delete(0, tk.END)
        self.cp_entry.insert(0, data.get("chromiumPath", ""))

        # Clients list
        self.cl_list.delete(0, tk.END)
        for c in data.get("clients", []):
            en = "\u2705" if c.get("enabled", True) else "\u274c"
            self.cl_list.insert(tk.END, f"{en} {c.get('id', '?')} - {c.get('name', '?')} [{c.get('phone', 'sem numero')}]")

        # Comboboxes
        cids = [c.get("id") for c in data.get("clients", [])]
        for cb_name in ["blc_cb", "hc_cb", "resp_cb", "mc_combo"]:
            cb = getattr(self, cb_name, None)
            if cb:
                cb["values"] = cids
                if cids: cb.set(cids[0])

    def _save_config_file(self):
        try:
            content = self.cl_edit.get("1.0", tk.END)
            json.loads(content)  # validate
            with open(os.path.join(BASE_DIR, "config.json"), "w", encoding="utf-8") as f:
                f.write(content)
            messagebox.showinfo("Salvo", "config.json salvo! Reinicie o servidor Node.js para aplicar.")
        except json.JSONDecodeError as e:
            messagebox.showerror("Erro JSON", f"JSON invalido: {e}")

    def _save_config(self):
        content = self.cl_edit.get("1.0", tk.END) if hasattr(self, 'cl_edit') else "{}"
        try:
            cfg = json.loads(content) if content.strip() else {}
        except:
            cfg = {}
        cfg["mode"] = self.mode_var.get()
        cfg["botEnabled"] = self.gb_var.get()
        cfg["vacationMode"] = self.gv_var.get()
        cfg["vacationMessage"] = self.vm_entry.get()
        cfg["botName"] = self.bn_entry.get()
        cfg["controllerNumber"] = self.cn_entry.get()
        try: cfg["humanResetHours"] = int(self.hr_entry.get())
        except: pass
        cfg["chromiumPath"] = self.cp_entry.get()
        with open(os.path.join(BASE_DIR, "config.json"), "w", encoding="utf-8") as f:
            json.dump(cfg, f, indent=2, ensure_ascii=False)
        messagebox.showinfo("Configuracao", "Salva com sucesso! Reinicie o servidor Node.js.")

    def _save_vacation_msg(self):
        pass

    def _toggle_global_bot(self):
        val = self.gb_var.get()
        r = api_post("/api/bot/toggle", {"enabled": val})
        if r.get("success"): self.gb_var.set(r.get("botEnabled", val))

    def _toggle_vacation(self):
        val = self.gv_var.get()
        msg = self.vm_entry.get()
        r = api_post("/api/vacation", {"enabled": val, "message": msg if val else ""})
        if r.get("success"): self.gv_var.set(r.get("vacationMode", val))

    def _on_client_select(self, e):
        sel = self.cl_list.curselection()
        if not sel: return
        txt = self.cl_list.get(sel[0])
        cid = txt.split(" - ")[0].split()[1] if " - " in txt else "principal"
        data = api_get("/api/config")
        if "error" in data: return
        for c in data.get("clients", []):
            if c.get("id") == cid:
                self.cl_edit.delete("1.0", tk.END)
                self.cl_edit.insert("1.0", json.dumps(c, indent=2, ensure_ascii=False))
                return

    # ================================================================
    # ACOES: BLACKLIST
    # ================================================================
    def _refresh_bl(self):
        cid = self.blc_var.get()
        r = api_get(f"/api/blacklist?client={cid}")
        self.bl_list.delete(0, tk.END)
        if "error" in r:
            self.bl_list.insert(tk.END, f"Erro: {r['error']}")
            return
        bl = r.get("blacklist", [])
        if isinstance(bl, list):
            for n in bl: self.bl_list.insert(tk.END, n)

    def _block_num(self):
        num = self.bl_entry.get().strip(); cid = self.blc_var.get()
        if not num: return
        r = api_post("/api/blacklist", {"number": num, "clientId": cid})
        if r.get("success"): self._refresh_bl(); self.bl_entry.delete(0, tk.END)

    def _unblock_num(self):
        num = self.bl_entry.get().strip(); cid = self.blc_var.get()
        if not num:
            sel = self.bl_list.curselection()
            if not sel: return
            num = self.bl_list.get(sel[0])
        r = api_delete("/api/blacklist", {"number": num, "clientId": cid})
        if r.get("success"): self._refresh_bl(); self.bl_entry.delete(0, tk.END)

    # ================================================================
    # ACOES: WEBHOOK
    # ================================================================
    def _set_wh(self):
        u = self.wh_var.get().strip()
        if not u: return
        r = api_post("/api/config/webhook", {"url": u})
        if r.get("success"): messagebox.showinfo("OK", "Webhook definido")

    def _del_wh(self):
        r = api_delete("/api/config/webhook")
        if r.get("success"): self.wh_var.set("")

    def _check_wh(self):
        r = api_get("/api/config/webhook")
        if r.get("webhook"): self.wh_var.set(r["webhook"])
        else: self.wh_var.set("")

    # ================================================================
    # ACOES: MODO HUMANO
    # ================================================================
    def _refresh_human(self):
        cid = self.hc_var.get()
        r = api_get(f"/api/human?client={cid}")
        self.hm_list.delete(0, tk.END)
        if "error" in r: return
        if isinstance(r, dict) and "list" in r:
            for e in r.get("list", []):
                n = e.get("number", "?")
                s = e.get("since", 0)
                ts = datetime.fromtimestamp(s / 1000).strftime("%H:%M") if s else "?"
                self.hm_list.insert(tk.END, f"{n}  (desde {ts})")

    def _hm_toggle(self, action):
        num = self.hm_entry.get().strip(); cid = self.hc_var.get()
        if not num: return
        r = api_post("/api/human", {"number": num, "action": action, "clientId": cid})
        if r.get("success"): self._refresh_human(); self.hm_entry.delete(0, tk.END)

    # ================================================================
    # ACOES: TEMPLATES
    # ================================================================
    def _refresh_templates(self):
        r = api_get("/api/templates")
        self.tp_list.delete(0, tk.END)
        for t in r.get("templates", []):
            self.tp_list.insert(tk.END, t.get("name", "?"))

    def _on_template_select(self, e):
        sel = self.tp_list.curselection()
        if not sel: return
        name = self.tp_list.get(sel[0])
        r = api_get("/api/templates")
        for t in r.get("templates", []):
            if t.get("name") == name:
                self.tp_name.delete(0, tk.END)
                self.tp_name.insert(0, name)
                self.tp_content.delete("1.0", tk.END)
                self.tp_content.insert("1.0", t.get("content", ""))
                return

    def _save_template(self):
        name = self.tp_name.get().strip()
        content = self.tp_content.get("1.0", tk.END).strip()
        if not name or not content: return
        r = api_post("/api/templates", {"name": name, "content": content})
        if r.get("success"): self._refresh_templates()

    def _del_template(self):
        name = self.tp_name.get().strip()
        if not name: return
        r = api_delete("/api/templates", {"name": name})
        if r.get("success"): self._refresh_templates(); self.tp_name.delete(0, tk.END); self.tp_content.delete("1.0", tk.END)

    def _use_template(self):
        content = self.tp_content.get("1.0", tk.END).strip()
        if content:
            self.nb.select(1)  # vai pra aba Enviar
            self.st.delete("1.0", tk.END)
            self.st.insert("1.0", content)

    # ================================================================
    # ACOES: AGENDAMENTOS
    # ================================================================
    def _refresh_schedules(self):
        r = api_get("/api/schedules")
        self.sch_list.delete(0, tk.END)
        for s in r.get("schedules", []):
            en = "\u2705" if s.get("enabled") else "\u23f3"
            self.sch_list.insert(tk.END, f"{en} {s.get('time', '??:??')} - {s.get('name', '?')} -> {s.get('phone', '?')}")

    def _add_schedule(self):
        d = {"name": self.sch_name.get().strip(), "phone": self.sch_phone.get().strip(), "time": self.sch_time.get().strip(), "message": self.sch_msg.get("1.0", tk.END).strip(), "clientId": self.sch_client.get()}
        if not d["phone"] or not d["time"] or not d["message"]: return
        r = api_post("/api/schedules", d)
        if r.get("success"): self._refresh_schedules()

    def _toggle_schedule(self):
        sel = self.sch_list.curselection()
        if not sel: return
        txt = self.sch_list.get(sel[0])
        r = api_get("/api/schedules")
        idx = sel[0]
        schedules = r.get("schedules", [])
        if idx < len(schedules):
            sid = schedules[idx]["id"]
            enabled = not schedules[idx].get("enabled", True)
            api_patch("/api/schedules", {"id": sid, "enabled": enabled})
            self._refresh_schedules()

    def _del_schedule(self):
        sel = self.sch_list.curselection()
        if not sel: return
        r = api_get("/api/schedules")
        schedules = r.get("schedules", [])
        if sel[0] < len(schedules):
            sid = schedules[sel[0]]["id"]
            api_delete("/api/schedules", {"id": sid})
            self._refresh_schedules()

    # ================================================================
    # ACOES: ESTATISTICAS
    # ================================================================
    def _refresh_stats(self):
        data = api_get("/api/stats")
        self.stats_text.configure(state=tk.NORMAL)
        self.stats_text.delete("1.0", tk.END)
        if "error" in data:
            self.stats_text.insert("1.0", f"Erro: {data['error']}")
        else:
            clients = data.get("clients", [])
            totals = data.get("totals", {})
            lines = [
                f"{'='*50}",
                f"  DDS: ZAP-API v3.0 - ESTATISTICAS",
                f"{'='*50}",
                f"",
                f"  Modo:      {data.get('mode', '?').upper()}",
                f"  Bot:       {'ATIVO' if data.get('botEnabled', True) else 'PAUSADO'}",
                f"  Ferias:    {'SIM' if data.get('vacationMode', False) else 'NAO'}",
                f"  Uptime:    {data.get('uptimeFormatted', '?')}",
                f"  Desde:     {data.get('startTime', '?')}",
                f"",
                f"  -- TOTAIS GLOBAIS --",
                f"  Recebidas: {totals.get('received', 0)}",
                f"  Enviadas:  {totals.get('sent', 0)}",
                f"  Midias:    {totals.get('media', 0)}",
                f"  Arquivos:  {data.get('mediaFiles', 0)}",
                f"",
                f"  -- POR CLIENTE --",
            ]
            for c in clients:
                lines.extend([
                    f"  [{c.get('id', '?')}] {'\u2705' if c.get('connected') else '\u274c'} Bot:{'ON' if c.get('botEnabled', True) else 'OFF'}",
                    f"     R:{c.get('received',0)} E:{c.get('sent',0)} M:{c.get('media',0)} | Humanos:{c.get('humanCount',0)} Blacklist:{c.get('blacklistCount',0)}",
                ])
            lines.append(f"{'='*50}")
            self.stats_text.insert("1.0", "\n".join(lines))
        self.stats_text.configure(state=tk.DISABLED)

    def _open_log(self):
        lp = os.path.join(BASE_DIR, "server.log")
        if os.path.exists(lp):
            os.startfile(lp) if os.name == 'nt' else None
        else:
            messagebox.showinfo("Log", "Arquivo server.log nao encontrado. O servidor cria um ao iniciar.")

    # ================================================================
    # ACOES: MENSAGENS
    # ================================================================
    def _refresh_msgs(self):
        filtro = self.mf.get().strip()
        cid = self.mc_var.get()
        params = f"?limit=40"
        if filtro: params += f"&from={filtro}"
        if cid: params += f"&client={cid}"
        data = api_get(f"/api/messages{params}")

        for r in self.mt.get_children(): self.mt.delete(r)
        if "error" in data: return

        for m in data.get("messages", []):
            self.mt.insert("", tk.END, values=(
                m.get("id", "")[:8],
                m.get("clientId", "?") if "clientId" in m else m.get("_clientId", "?"),
                m.get("from", ""),
                m.get("body", "")[:90],
                "\u2705" if m.get("hasMedia") else "",
                m.get("formattedTime", "")[11:19] if m.get("formattedTime") else "",
            ))

    def _clear_msgs(self):
        if messagebox.askyesno("Limpar", "Limpar historico de mensagens?"):
            api_delete("/api/messages")
            self._refresh_msgs()

    def _refresh_convs(self):
        data = api_get("/api/messages?limit=500")
        if "error" in data: return
        convs = {}
        for m in data.get("messages", []):
            from_num = m.get("from_number", m.get("from", "?"))
            if from_num not in convs:
                convs[from_num] = {"msgs": [], "last": 0, "count": 0}
            convs[from_num]["msgs"].append(m)
            convs[from_num]["last"] = max(convs[from_num]["last"], m.get("timestamp", 0))
            convs[from_num]["count"] += 1
        sorted_convs = sorted(convs.items(), key=lambda x: x[1]["last"], reverse=True)
        self._convs_data = {k: v for k, v in sorted_convs}
        self.conv_list.delete(0, tk.END)
        for from_num, cdata in sorted_convs:
            last_msg = cdata["msgs"][-1].get("body", "(midia)")[:40]
            t = datetime.fromtimestamp(cdata["last"]).strftime("%d/%m %H:%M")
            self.conv_list.insert(tk.END, f"{from_num}  ({cdata['count']} msg)  {t}  {last_msg}")

    def _on_conv_select(self, e):
        sel = self.conv_list.curselection()
        if not sel: return
        idx = sel[0]
        from_nums = list(self._convs_data.keys())
        if idx >= len(from_nums): return
        from_num = from_nums[idx]
        msgs = self._convs_data[from_num]["msgs"]
        self.conv_detail.configure(state=tk.NORMAL)
        self.conv_detail.delete("1.0", tk.END)
        for m in reversed(msgs):
            who = "Eu" if m.get("from_me") else m.get("from_number", "?")
            t = datetime.fromtimestamp(m.get("timestamp", 0)).strftime("%H:%M")
            body = m.get("body", "(midia)")
            self.conv_detail.insert(tk.END, f"[{t}] {who}: {body}\n")
        self.conv_detail.configure(state=tk.DISABLED)

    # ================================================================
    # EVENTO WEBHOOK (via WebSocket)
    # ================================================================
    def _on_ws_event(self, event, data):
        if event == "status":
            cid = data.get("client_id", "?")
            connected = data.get("connected", False)
            if cid in self.conn_cards:
                s = self.conn_cards[cid]["status"]
                if connected:
                    s.configure(text=f"\u2705 Conectado - {cid}", foreground="#2ecc71")
                else:
                    s.configure(text=f"\u274c Desconectado - {cid}", foreground="#e74c3c")
            self._update_ws_indicator()
        elif event == "qr":
            cid = data.get("client_id", "?")
            qr = data.get("qr", "")
            if cid in self.conn_cards:
                self._display_qr_card(cid, qr)
        elif event == "message":
            # Nova mensagem chegou - pode atualizar aba se estiver visivel
            self.root.after(0, self._refresh_msg_count)

    def _update_ws_indicator(self):
        ws_on = WS_CONNECTED[0]
        self.st_ws.configure(text="WS On" if ws_on else "HTTP")
        self.st_ws.configure(foreground="#2ecc71" if ws_on else "gray")

    def _refresh_msg_count(self):
        pass  # placeholder - msg tab ja atualiza com poll

    # ================================================================
    # POLLING (fallback quando WS desconecta)
    # ================================================================
    def _start_polling(self):
        def poll():
            while self._polling:
                try:
                    r = api_get("/status")
                    if "error" not in r:
                        self.root.after(0, lambda: self._update_status(r))
                        self.root.after(0, lambda: self._rebuild_conn_cards(r))
                        # Update client combos
                        clients = [c["id"] for c in r.get("clients", [])]
                        for cb_name in ["mc_combo", "resp_cb", "sch_client"]:
                            cb = getattr(self, cb_name, None)
                            if cb:
                                vals = cb["values"]
                                if list(vals) != clients:
                                    self.root.after(0, lambda cb=cb, cl=clients: cb.configure(values=cl))
                    # Poll menos quando WS esta ativo (só pra combo/config)
                    interval = 30 if WS_CONNECTED[0] else 15
                    for _ in range(interval):
                        if not self._polling: return
                        time.sleep(1)
                except: time.sleep(5)
        threading.Thread(target=poll, daemon=True).start()

    def _update_status(self, data):
        clients = data.get("clients", [])
        connected = any(c.get("connected") for c in clients)
        ready = any(c.get("ready") for c in clients)

        if connected and ready:
            self.st_dot.configure(foreground="#2ecc71")
            self.st_label.configure(text=f"Conectado | {sum(1 for c in clients if c.get('ready'))}/{len(clients)} clientes ativos")
        elif any(c.get("ready") for c in clients):
            self.st_dot.configure(foreground="#f39c12")
            self.st_label.configure(text="Parcialmente conectado")
        else:
            self.st_dot.configure(foreground="#e74c3c")
            self.st_label.configure(text="Desconectado (escaneie QR)")

        self.st_mode.configure(text=f"Modo: {data.get('mode', '?').upper()}")
        ai_provider = data.get('aiProvider', 'openai')
        has_ai = data.get('ai', False)
        ai_txt = f"IA:{ai_provider}" if has_ai else "IA:off"
        self.st_info.configure(text=ai_txt)
        self.st_ws.configure(text="WS" if WS_CONNECTED[0] else "HTTPS")
        self.st_server.configure(text="Servidor: ON" if SERVER_RUNNING[0] else "Servidor: OFF")
        self.server_btn.configure(text="\u23f9 Parar Servidor" if SERVER_RUNNING[0] else "\u25b6 Iniciar Servidor")
        self.restart_btn.configure(state=tk.NORMAL if SERVER_RUNNING[0] else tk.DISABLED)

    def on_close(self):
        self._polling = False
        WS_CONNECTED[0] = False
        WS_APP[0] = None
        _stop_server()
        self.root.destroy()

# ================================================================
if __name__ == "__main__":
    root = tk.Tk()
    root.title("DDS: ZAP-API v3.0 - Painel de Controle WhatsApp")
    root.geometry("1100x720")
    root.minsize(900, 600)

    app = ZapGUI(root)

    # Configuracao inicial se .env vazio ou nao existe
    if _need_setup():
        root.after(200, lambda: _run_setup(root))

    # Dialogo para iniciar servidor (so pergunta se config OK)
    root.after(500, lambda: _show_startup_dialog(root, app))

    root.protocol("WM_DELETE_WINDOW", app.on_close)
    root.after(2000, app._load_config_gui)
    root.after(2400, app._refresh_bl)
    root.after(2600, app._refresh_human)
    root.after(2800, app._refresh_msgs)
    root.after(1000, app._refresh_stats)
    root.after(1200, app._refresh_templates)
    root.after(1400, app._refresh_schedules)

    # Polling do status do servidor
    def _check_server():
        _update_btns()
        root.after(3000, _check_server)

    root.after(3000, _check_server)

    root.mainloop()
