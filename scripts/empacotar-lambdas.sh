#!/usr/bin/env bash
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"

if [[ -f "$raiz/.env" ]]; then
  set -a
  source "$raiz/.env"
  set +a
fi

arquitetura="${LAMBDA_ARCHITECTURE:-x86_64}"
if [[ "$arquitetura" != "x86_64" && "$arquitetura" != "arm64" ]]; then
  echo "Erro: LAMBDA_ARCHITECTURE inválida '$arquitetura'. Valores aceitos: x86_64 ou arm64." >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Erro: dotnet não foi encontrado. Instale o .NET 10 SDK." >&2
  exit 1
fi

if ! dotnet lambda --help >/dev/null 2>&1; then
  echo "Erro: Amazon.Lambda.Tools não foi encontrado. Instale com: dotnet tool install -g Amazon.Lambda.Tools" >&2
  exit 1
fi

mkdir -p "$raiz/dist"

lambdas="motor-regras-score:src/MotorCredito.Lambda.MotorRegrasScore
processador-proposta:src/MotorCredito.Lambda.ProcessadorProposta
despachante-notificacao:src/MotorCredito.Lambda.DespachanteNotificacao
consulta-proposta:src/MotorCredito.Lambda.ConsultaProposta"

while IFS= read -r lambda; do
  nome="${lambda%%:*}"
  projeto="${lambda#*:}"
  echo "Empacotando $nome..."
  dotnet lambda package \
    --project-location "$raiz/$projeto" \
    --configuration Release \
    --framework net10.0 \
    --function-architecture "$arquitetura" \
    --output-package "$raiz/dist/$nome.zip"
done <<< "$lambdas"

echo "Pacotes gerados:"
ls -1 "$raiz"/dist/*.zip
