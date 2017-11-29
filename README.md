## Database-as-a-Service demo

A quick-and-dirty PaaS implementation using SQL Server in Linux containers on Kubernetes.

### Requirements

* .NET Core 2.0
* A private Docker registry  
  e.g. Azure Container Registry, or quay.io
* A Kubernetes cluster  
  If you don't have one yet, see the [deploy/terraform/ddcloud](deploy/terraform/ddcloud) and [deploy/ansible](deploy/ansible) directories for some scripts you can use to bring up a cluster managed by Rancher in Dimension Data's cloud.
* A server for storage  
  This server will need to export an NFS volume unless you have your own options for storage
* A DNS `A` wildcard record pointing to your cluster nodes' public IPs

### Deployment

#### Images

Run `.\Build-Images.ps1` or `./build-images.sh`.

#### Kubernetes Resources

Customise the files in [deploy/k8s](deploy/k8s) as required, and run `kubectl create -f deploy/k8s/XXX`, where `XXX` is:

* `rook`
* `ravendb`
* `elasticsearch`
* `prometheus`

Then run `deploy/k8s/consul/install.sh`, `deploy/k8s/vault/install-vault.sh`, and `kubectl create -f deploy/k8s/vault/vault-svc.yml`.

Configure Vault:

* `vault mount -path=/daas/pki pki`
* `vault mount-tune -max-lease-ttl=87600h /daas/pki`
* `vault write /daas/pki/root/generate/internal common_name=vault.<cluster-fqdn> ttl=87600h`
* `vault write /daas/pki/roles/daas.server.database allowed_domains=database.<cluster-fqdn> allow_subdomains=true allow_bare_domains=true max_ttl=672h`
* `vault write /daas/pki/roles/daas.user.database allowed_domains=database.<cluster-fqdn> allow_subdomains=true max_ttl=672h`

Finally, run `kubectl create -f deploy/k8s`.
