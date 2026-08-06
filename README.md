# SOEN Radiadores Torrezim

Sistema de gestão para oficina de radiadores (SOEN). Desenvolvido em **C# WinForms (.NET Framework 4.7.2)**.

> Primeiro projeto desenvolvido do zero pelo autor. Documentação completa na pasta `docs/` — sempre consultar antes de mudar o código.

## Status

- ✅ Esqueleto de interface com menu principal
- ✅ SQLite (banco local) + padrão DAO implementado
- ✅ Cadastro de Clientes salvando no banco
- ✏️ Consulta de Clientes em desenvolvimento
- ⬜ Demais módulos em desenvolvimento

## Estrutura de pastas

```
docs/                  # Documentação do projeto (LEIA ANTES DE MEXER)
├── MAPA_MENU.md           # Estrutura dos menus e telas
├── PLANO_DESENVOLVIMENTO.md  # Fases de desenvolvimento (checklist)
├── BANCO_DE_DADOS.md      # Esquema do banco SQLite
└── GUIA_ARQUITETURA.md    # Padrão de código (DAO, modelos, telas)
```

## Como rodar

1. Abra `Soen - Torrezim.sln` no Visual Studio (2017+).
2. Build + Run (F5).

O banco de dados é criado automaticamente no primeiro uso (SQLite local, sem servidor).

## Docs

- [Mapa dos menus](docs/MAPA_MENU.md)
- [Plano de desenvolvimento](docs/PLANO_DESENVOLVIMENTO.md)
- [Banco de dados](docs/BANCO_DE_DADOS.md)
- [Guia de arquitetura](docs/GUIA_ARQUITETURA.md)
