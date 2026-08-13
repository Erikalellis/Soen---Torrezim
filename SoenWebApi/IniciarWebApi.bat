@echo off
setlocal EnableExtensions
rem ============================================================
rem  SOEN - TORREZIM | Iniciar SoenWebApi (WhatsApp)
rem  Inicia o servidor local da webapi na porta configurada,
rem  usando o Node.js portatil embutido (node\node.exe) --
rem  nao precisa de Node instalado no sistema.
rem  No primeiro uso, leia o QR Code pelo painel:
rem    http://localhost:3000/admin
rem  Log: server.log (na pasta desta webapi)
rem ============================================================
title SoenWebApi - WhatsApp (SOEN)
set "AQUI=%~dp0"
if "%AQUI:~-1%"=="\" set "AQUI=%AQUI:~0,-1%"
cd /d "%AQUI%"

set "NODE=%AQUI%\node\node.exe"
if exist "%NODE%" goto :tem_node
echo  [ERRO] Node.js portatil nao encontrado em:
echo         %NODE%
echo  Reinstale o pacote completo - Instalar.bat - a partir do pendrive.
echo.
pause
exit /b 1

:tem_node
set "CHROM=%AQUI%\ChromiumPortable\App\Chromium\chrome.exe"

rem ---- gera .env na primeira execucao (inclui CHROME_PATH absoluto) ----
if not exist "%AQUI%\.env" goto :gera_env
goto :tem_env

:gera_env
if not exist "%AQUI%\.env.example" goto :tem_env
copy /y "%AQUI%\.env.example" "%AQUI%\.env" >nul 2>nul
if exist "%CHROM%" powershell -NoProfile -Command "$envfile='%AQUI%\.env'; $t=Get-Content $envfile -Raw; $t=$t -replace '(?m)^CHROME_PATH=.*$', ('CHROME_PATH=' + '%CHROM%'.Replace('\','/')); Set-Content -Path $envfile -Value $t" >nul 2>nul

:tem_env
echo  Iniciando SoenWebApi em %AQUI% ...
echo  Aguarde alguns segundos para o WhatsApp conectar - Chromium.
echo  Painel: http://localhost:3000/admin
echo  Fechar esta janela encerra o servico.
echo.
"%NODE%" "index.js"
endlocal