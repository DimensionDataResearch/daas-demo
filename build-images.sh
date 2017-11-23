#!/bin/bash

set -euo pipefail

COMPONENTS='api provisioning sql-executor ui'
REPO=tintoy.azurecr.io/daas
TAG=${1:-1.0.0-dev}

for COMPONENT in $COMPONENTS; do
  IMAGE="${REPO}/${COMPONENT}:$TAG"
  echo "Building image '$IMAGE'..."  
  
  docker build -t $IMAGE -f "./Dockerfile.${COMPONENT}" .
done

for COMPONENT in $COMPONENTS; do
  IMAGE="${REPO}/${COMPONENT}:$TAG"

  echo "Pushing image '$IMAGE'..."  
  docker push $IMAGE
done

echo "Done."
