#!/usr/bin/env bash
# ==============================================================================
# Rota Leve - Setup do projeto no Azure DevOps (projeto NOVO e PRIVADO)
# Cria projeto, importa o repo para o Azure Repos, protege a main e registra
# as pipelines de Build (CI) e Release (CD).
#
# Pre-requisito: az extension add --name azure-devops ; az login
#   (autentique com `az devops login` digitando o PAT no prompt; nunca em chat)
#
#   export ORG_URL="https://dev.azure.com/<sua-org>"
#   export REVIEWER_EMAIL="<seu-email-fiap>"     # revisor padrao (seu RM)
#   export SOURCE_REPO_URL="https://github.com/<org>/<repo-rota-leve>"  # opcional
# ==============================================================================
set -euo pipefail

: "${ORG_URL:?Defina ORG_URL}"
: "${REVIEWER_EMAIL:?Defina REVIEWER_EMAIL}"
PROJECT="${PROJECT:-GS-RotaLeve}"
REPO="${REPO:-rota-leve-api}"

az devops configure --defaults organization="$ORG_URL"

# 1) Projeto novo, privado, Git + processo Scrum (Epic > Feature > PBI > Task)
if ! az devops project show -p "$PROJECT" -o none 2>/dev/null; then
  az devops project create --name "$PROJECT" --visibility private \
    --source-control git --process Scrum \
    --description "GS 2026/2 CitizenTech - Rota Leve" -o none
fi
az devops configure --defaults organization="$ORG_URL" project="$PROJECT"

# 2) Azure Repos
if ! az repos show -r "$REPO" -o none 2>/dev/null; then
  az repos create --name "$REPO" -o none
fi
if [[ -n "${SOURCE_REPO_URL:-}" ]]; then
  # repo publico: import direto. Privado: use --requires-authorization
  az repos import create --git-source-url "$SOURCE_REPO_URL" -r "$REPO" -o none || \
    echo "Import falhou (repo ja tem commits?). Faca git push manual para o Azure Repos."
fi
REPO_ID=$(az repos show -r "$REPO" --query id -o tsv)

# 3) Politicas da main (edital req. 03 / penalidade -20)
#    - minimo 1 revisor, criador pode aprovar (req. 05: aluno aprova a propria PR)
az repos policy approver-count create --repository-id "$REPO_ID" --branch main \
  --blocking true --enabled true --minimum-approver-count 1 \
  --creator-vote-counts true --allow-downvotes false --reset-on-source-push false -o none
#    - work item vinculado obrigatorio
az repos policy work-item-linking create --repository-id "$REPO_ID" --branch main \
  --blocking true --enabled true -o none
#    - revisor padrao (voce)
az repos policy required-reviewer create --repository-id "$REPO_ID" --branch main \
  --blocking true --enabled true --required-reviewer-ids "$REVIEWER_EMAIL" \
  --message "Revisor padrao do RM" -o none

# 4) Pipelines (YAML no repo). Build: azure-pipeline.yml na raiz.
az pipelines create --name "RotaLeve-Build-CI" --repository "$REPO" --repository-type tfsgit \
  --branch main --yml-path azure-pipeline.yml --skip-first-run true -o none
az pipelines create --name "RotaLeve-Release-CD" --repository "$REPO" --repository-type tfsgit \
  --branch main --yml-path pipelines/release.yml --skip-first-run true -o none

echo "Pronto. Falta no portal: Variable Group 'rotaleve-secrets', service connections"
echo "'sc-azure-rotaleve' (ARM, escopo no RG) e 'sc-acr-rotaleve' (Docker Registry),"
echo "environment 'producao' e convidar o professor (Basic na org + Project Administrators)."
