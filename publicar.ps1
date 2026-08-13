#Requires -Version 5.1
<#
.SYNOPSIS
  Publica uma nova versão do Soen no GitHub Releases.
.DESCRIPTION
  Faz bump de versão no AssemblyInfo.cs, compila Release, empacota os binários
  (sem soen.db / Backups), atualiza dist\conteudo e cria a Release (tag vX.Y.Z)
  com o ZIP de distribuição.
.PARAMETER Versao
  Versão no formato X.Y.Z (ex.: 1.2.0). Se omitida, incrementa o patch da
  versão atual do AssemblyInfo.
.PARAMETER Notas
  Texto das notas da Release. Se omitido, gera um resumo automático.
.PARAMETER Draft
  Cria a Release como rascunho (não publica para os usuários).
.PARAMETER SemGit
  Não cria tag nem envia para o GitHub (só build + zip local).
.EXAMPLE
  .\publicar.ps1 -Versao 1.2.0
  .\publicar.ps1 -Draft -Notas "Correções de testes"
  .\publicar.ps1 -SemGit
#>

[CmdletBinding()]
param(
    [string]$Versao = "",
    [string]$Notas = "",
    [switch]$Draft,
    [switch]$SemGit
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------- caminhos
$Raiz        = Split-Path -Parent $MyInvocation.MyCommand.Path
$Projeto     = Join-Path $Raiz "Soen - Torrezim.csproj"
$Solucao     = Join-Path $Raiz "Soen - Torrezim.sln"
$AssemblyInfo = Join-Path $Raiz "Properties\AssemblyInfo.cs"
$BinDir      = Join-Path $Raiz "bin\Release"
$DistDir     = Join-Path (Split-Path -Parent $Raiz) "dist"
$ConteudoDir = Join-Path $DistDir "conteudo"
$Staging     = Join-Path $env:TEMP ("soen_pub_" + [Guid]::NewGuid().ToString("N"))

$ExeName    = "Soen - Torrezim.exe"
$RepoDono   = "Erikalellis"
$RepoNome   = "Soen---Torrezim"
$GhExe      = "C:\Program Files\GitHub CLI\gh.exe"

# Arquivos publicáveis (mantém o mesmo conjunto do ZIP da v1.1.0).
$ArquivosFixos = @(
    "Soen - Torrezim.exe",
    "Soen - Torrezim.exe.config",
    "System.Data.SQLite.dll",
    "System.Resources.Extensions.dll"
)
$PastasFixas = @("x64", "x86")

function Escreve-Linha { param([string]$Msg) Write-Host "==> $Msg" }

# ---------------------------------------------------------------- versão
if (-not $Versao) {
    $atual = (Select-String -Path $AssemblyInfo -Pattern 'AssemblyVersion\("([0-9]+)\.([0-9]+)\.([0-9]+)(\.([0-9]+))?"\)').Matches.Groups
    $m = Select-String -Path $AssemblyInfo -Pattern 'AssemblyVersion\("(\d+)\.(\d+)\.(\d+)' | Select-Object -First 1
    if (-not $m) { throw "Nao encontrei AssemblyVersion no AssemblyInfo.cs" }
    $maior  = [int]$m.Matches[0].Groups[1].Value
    $menor  = [int]$m.Matches[0].Groups[2].Value
    $patch  = [int]$m.Matches[0].Groups[3].Value
    $Versao = "$maior.$menor.$($patch + 1)"
    Escreve-Linha "Versao nao informada; incrementando patch para $Versao"
} elseif ($Versao -notmatch '^\d+\.\d+\.\d+$') {
    throw "Versao invalida '$Versao'. Use X.Y.Z (ex.: 1.2.0)."
}

$Tag = "v$Versao"
Escreve-Linha "Publicando versao $Versao (tag $Tag)"

# ---------------------------------------------------------------- bump
Escreve-Linha "Atualizando AssemblyInfo.cs para $Versao.0.0"
$navigar = Get-Content $AssemblyInfo -Raw
$navigar = $navigar -replace 'AssemblyVersion\("\d+\.\d+\.\d+(\.\d+)?"\)', "AssemblyVersion(`"$Versao.0`")"
$navigar = $navigar -replace 'AssemblyFileVersion\("\d+\.\d+\.\d+(\.\d+)?"\)', "AssemblyFileVersion(`"$Versao.0`")"
Set-Content -Path $AssemblyInfo -Value $navigar -Encoding UTF8

# ---------------------------------------------------------------- build
Escreve-Linha "Build Release (Release|AnyCPU)..."
& dotnet build $Solucao -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "Build falhou (exit $LASTEXITCODE)" }
if (-not (Test-Path (Join-Path $BinDir $ExeName))) { throw "Nao achei $ExeName em $BinDir" }

# ---------------------------------------------------------------- empacotar
Escreve-Linha "Empacotando conteudo em staging..."
New-Item -ItemType Directory -Path $Staging -Force | Out-Null
foreach ($f in $ArquivosFixos) {
    $orig = Join-Path $BinDir $f
    if (Test-Path $orig) { Copy-Item $orig (Join-Path $Staging (Split-Path $f -Leaf)) -Force }
    else { Write-Warning "Arquivo fixo nao encontrado: $f (ignorado)" }
}
foreach ($p in $PastasFixas) {
    $orig = Join-Path $BinDir $p
    if (Test-Path $orig) { Copy-Item $orig (Join-Path $Staging $p) -Recurse -Force }
    else { Write-Warning "Pasta nao encontrada: $p (ignorado)" }
}

# --- valida intervalo de tamanho e lista final
$ql = Get-ChildItem $Staging -Recurse -File
$tot = ($ql | Measure-Object -Property Length -Sum).Sum
Escreve-Linha ("Staging: {0} arquivo(s), {1:N0} bytes" -f $ql.Count, $tot)
if ($tot -lt 1000000) { throw "ZIP parece truncado (< 1 MB) - abortando." }
$semExt = Get-ChildItem $Staging -Recurse -File | Where-Object { $_.Extension -in ".db",".pdb" }
if ($semExt) { Write-Warning "ATENCAO: encontrei $($semExt.Count) arquivo(s) .db/.pdb no staging (nao publicados)." }

# ---------------------------------------------------------------- dist\conteudo
Escreve-Linha "Atualizando dist\conteudo..."
if (-not (Test-Path $ConteudoDir)) { New-Item -ItemType Directory -Path $ConteudoDir -Force | Out-Null }
foreach ($f in $ArquivosFixos) {
    $orig = Join-Path $BinDir $f
    if (Test-Path $orig) { Copy-Item $orig (Join-Path $ConteudoDir (Split-Path $f -Leaf)) -Force }
}
foreach ($p in $PastasFixas) {
    $orig = Join-Path $BinDir $p
    if (Test-Path $orig) { Remove-Item (Join-Path $ConteudoDir $p) -Recurse -Force -ErrorAction SilentlyContinue; Copy-Item $orig (Join-Path $ConteudoDir $p) -Recurse -Force }
}

# ---------------------------------------------------------------- webapi (repo -> pendrive)
# Os fonte da SoenWebApi vivem no repo (fonte de verdade). Os binários
# (node, ChromiumPortable, node_modules) existem apenas no pacote completo do
# pendrive. Este passo sincroniza os fontes corrigidos do repo para o pendrive,
# preservando os binários — evitando propagação manual arquivo a arquivo.
$WebApiRepo   = Join-Path $Raiz "SoenWebApi"
$PendriveWeb  = Join-Path $DistDir "pendrive\SOEN - TORREZIM Pendrive\SoenWebApi"
if (Test-Path $WebApiRepo -and (Test-Path (Join-Path $PendriveWeb "node\node.exe"))) {
    Escreve-Linha "Sincronizando SoenWebApi (repo -> pendrive)..."
    foreach ($dir in @("src", "public", "docs")) {
        $origDir = Join-Path $WebApiRepo $dir
        if (Test-Path $origDir) {
            New-Item -ItemType Directory -Path (Join-Path $PendriveWeb $dir) -Force | Out-Null
            Copy-Item (Join-Path $origDir "*") (Join-Path $PendriveWeb $dir) -Recurse -Force
        }
    }
    foreach ($f in @("index.js","app.py","swagger.json","README.md","package.json","package-lock.json",".env.example",".dockerignore","docker-compose.yml","Dockerfile","IniciarWebApi.bat","config.json")) {
        $orig = Join-Path $WebApiRepo $f
        if (Test-Path $orig) { Copy-Item $orig (Join-Path $PendriveWeb $f) -Force }
    }
    Escreve-Linha "SoenWebApi sincronizada (fontes corrigidos)."
} else {
    Write-Warning "Pendrive com webapi (node\node.exe) nao encontrado em '$PendriveWeb'; pulando sincronizacao."
}

# ---------------------------------------------------------------- zip
$ZipName = "Soen-Torrezim-$Tag.zip"
$ZipPath = Join-Path $DistDir $ZipName
Escreve-Linha "Criando zip $ZipPath..."
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path (Join-Path $Staging "*") -DestinationPath $ZipPath -CompressionLevel Optimal
"$ZipPath" | Set-Content (Join-Path $DistDir "ultimo_zip.txt")
Escreve-Linha ("ZIP {0:N0} bytes -> {1}" -f (Get-Item $ZipPath).Length, $ZipPath)

Remove-Item $Staging -Recurse -Force -ErrorAction SilentlyContinue

if ($SemGit) {
    Escreve-Linha "Modo -SemGit: tag e Release NAO criadas. Zip pronto em:"
    Write-Host "    $ZipPath"
    exit 0
}

# ---------------------------------------------------------------- git
Escreve-Linha "Commit e tag git..."
git add "Properties\AssemblyInfo.cs"
git commit -m "bump: versao $Versao" --allow-empty
if ($LASTEXITCODE -ne 0) { throw "git commit falhou" }
if (git tag -l "$Tag") { Write-Warning "Tag $Tag ja existe localmente; reutilizando" }
else { git tag "$Tag"; if ($LASTEXITCODE -ne 0) { throw "git tag falhou" } }

# ---------------------------------------------------------------- gh
Escreve-Linha "Autenticando gh..."
& $GhExe auth status 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw "gh nao esta autenticado. Rode 'gh auth login'." }

Escreve-Linha "Verificando versao atual exe = $Versao.0.0..."
$exeNorm = Join-Path $BinDir $ExeName
$vReal = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exeNorm).FileVersion
if ($vReal -ne "$Versao.0.0") {
    Write-Warning "exe reporta '$vReal' (esperado '$Versao.0.0'). Continua mesmo assim; confira o AssemblyInfo."
}

if (-not $Notas) {
    $Notas = "Versao $Versao do SOEN - Sistema de Ordem de Servico."
    if ($Draft) { $Notas += "  (RASCUNHO - nao publicada)" }
}

Escreve-Linha "Criando Release $Tag..."
$argsGh = @("release", "create", $Tag, $ZipPath, "--repo", "$RepoDono/$RepoNome", "--title", "Soen - Torrezim $Tag", "--notes", $Notas)
if ($Draft) { $argsGh += "--draft" }
& $GhExe @argsGh
if ($LASTEXITCODE -ne 0) { throw "gh release create falhou (exit $LASTEXITCODE)" }

Escreve-Linha "Push de branch e tag..."
git push origin "HEAD:$((git branch --show-current))" --set-upstream
git push origin "$Tag"

Escreve-Linha "Concluido! Release: https://github.com/$RepoDono/$RepoNome/releases/tag/$Tag"
Escreve-Linha ("Atualize as instalacoes locais em Nolt-DDS e Erika-pc copiando os arquivos de dist\conteudo (mantendo soen.db).")