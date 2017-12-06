#!/bin/bash

set -euo pipefail

# Make sure you've already set up ~/.kube/config

STORAGE_NODES=$(kubectl get nodes | grep kube-storage | awk '{ print $1 }')

for STORAGE_NODE in $STORAGE_NODES; do
    kubectl taint node $STORAGE_NODE role=storage:NoSchedule
done
