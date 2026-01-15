#!/usr/bin/env bash
# setup.sh
# Uso: bash setup.sh
set -euo pipefail

echo "== Setup do ambiente de desenvolvimento – MicroondasDigital =="

# Verifica se dotnet está instalado
if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet não encontrado. Instale o .NET SDK e tente novamente." >&2
  exit 1
fi

echo "1) Gerando e confiando no certificado de desenvolvimento (dotnet dev-certs https --trust)"
if dotnet dev-certs https --trust >/dev/null 2>&1; then
  echo "→ Certificado de desenvolvimento confiado."
else
  echo "→ 'dotnet dev-certs https --trust' pode requerer ação manual (no macOS pode abrir um prompt para confiar no certificado)." >&2
fi

# Exporta variáveis para a sessão atual (somente para o shell onde o script for executado)
export MICROONDAS_API_URL="https://localhost:7198/"
export MICROONDAS_ALLOW_INSECURE="0"

echo
echo "2) Variáveis de ambiente definidas para esta sessão:"
echo "   MICROONDAS_API_URL = ${MICROONDAS_API_URL}"
echo "   MICROONDAS_ALLOW_INSECURE = ${MICROONDAS_ALLOW_INSECURE}"

echo
echo "3) Como iniciar (em terminais separados):"
echo "   API -> dotnet run --project src/Microondas.API/Microondas.API.csproj"
echo "   UI  -> dotnet run --project src/Microondas.UI/Microondas.UI.csproj"

cat <<EOF

Observações:
 - Este script exporta variáveis apenas para a sessão atual. Para persistir, adicione as linhas export MICROONDAS_API_URL=... ao seu ~/.bashrc, ~/.zshrc ou similar.
 - Se preferir forçar HTTP (sem certificado), defina MICROONDAS_API_URL=http://localhost:5123/ antes de executar a UI.
 - No macOS o comando 'dotnet dev-certs https --trust' pode abrir um diálogo do sistema pedindo confirmação.

== Pronto ==
EOF