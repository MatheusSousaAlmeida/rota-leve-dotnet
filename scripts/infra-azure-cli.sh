#!/usr/bin/env bash
# ==============================================================================
# Rota Leve - Provisionamento da infraestrutura via Azure CLI (GS 2026/2)
# Cria: Resource Group, Azure Container Registry e (opcional) Service Principal
#       com Contributor restrito ao RG para a service connection do Azure DevOps.
# O container group (Oracle + API) e criado pela Pipeline de Release usando estes
# recursos, conforme exige o edital.
#
# Uso:
#   export SUBSCRIPTION_ID="<id-da-assinatura>"   # assinatura com credito
#   export RM="562012"
#   ./scripts/infra-azure-cli.sh            # cria RG + ACR
#   ./scripts/infra-azure-cli.sh --sp       # + service principal (saida NAO vai p/ chat/git)
#   ./scripts/infra-azure-cli.sh --destroy  # apaga SO o container group (ACR fica)
# ==============================================================================
set -euo pipefail

: "${SUBSCRIPTION_ID:?Defina SUBSCRIPTION_ID}"
: "${RM:?Defina RM (ex.: 562012)}"

LOCATION="${LOCATION:-mexicocentral}"          # regiao permitida pela policy do Students
RG="${RG:-rg-rotaleve-gs}"
ACR="${ACR:-acrrotaleve${RM}}"                 # so minusculas/numeros, unico global
CG="${CG:-cg-rotaleve}"
SP_NAME="${SP_NAME:-sp-rotaleve-devops}"

log() { printf '\033[1;36m[INFO]\033[0m %s\n' "$*"; }

az account set --subscription "$SUBSCRIPTION_ID"
log "Assinatura ativa: $(az account show --query name -o tsv)"

if [[ "${1:-}" == "--destroy" ]]; then
  log "Apagando apenas o container group $CG (ACR e RG permanecem)"
  az container delete -g "$RG" -n "$CG" --yes || true
  exit 0
fi

# Providers usados (idempotente)
for ns in Microsoft.ContainerRegistry Microsoft.ContainerInstance; do
  log "Registrando provider $ns"
  az provider register -n "$ns" --wait
done

log "Resource Group $RG em $LOCATION"
az group create -n "$RG" -l "$LOCATION" \
  --tags projeto=rota-leve disciplina=devops gs=2026-2 -o none

if ! az acr show -n "$ACR" -g "$RG" -o none 2>/dev/null; then
  log "Criando ACR $ACR (Basic)"
  # admin-enabled: o ACI precisa de credencial para puxar a imagem privada
  az acr create -n "$ACR" -g "$RG" -l "$LOCATION" --sku Basic --admin-enabled true -o none
else
  log "ACR $ACR ja existe"
fi

if [[ "${1:-}" == "--sp" ]]; then
  RG_ID=$(az group show -n "$RG" --query id -o tsv)
  log "Criando SP $SP_NAME com Contributor SOMENTE no RG (menor privilegio)"
  log "A saida abaixo e secreta: copie direto para a service connection, nao cole em chat/git"
  az ad sp create-for-rbac --name "$SP_NAME" --role Contributor --scopes "$RG_ID"
fi

log "Resumo:"
az resource list -g "$RG" --query "[].{nome:name,tipo:type,regiao:location}" -o table
echo "ACR login server: $(az acr show -n "$ACR" --query loginServer -o tsv)"
