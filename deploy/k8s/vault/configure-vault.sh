#!/bin/bash

# Enable CA
vault mount -path=/daas/pki pki
vault mount-tune -max-lease-ttl=87600h /daas/pki

# Generate CA certificate
vault write /daas/pki/root/generate/internal common_name=vault.kr-cluster.tintoy.io ttl=87600h

# Configure CA rolees
vault write /daas/pki/roles/daas.server.database allowed_domains=database.kr-cluster.tintoy.io allow_subdomains=true allow_bare_domains=true max_ttl=672h
vault write /daas/pki/roles/daas.user.database allowed_domains=database.kr-cluster.tintoy.io allow_subdomains=true max_ttl=672h
