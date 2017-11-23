## Database-as-a-Service demo

A quick-and-dirty PaaS implementation using SQL Server in Linux containers on Kubernetes.

### Requirements

* .NET Core 2.0
* An instance of SQL Server to host the management database  
  SQL Azure or SQL Server for Linux can also be used.
* A private Docker registry  
  e.g. Azure Container Registry, or quay.io
* A Kubernetes cluster  
  If you don't have one yet, see the [deploy/terraform/ddcloud](deploy/terraform/ddcloud) and [deploy/ansible](deploy/ansible) directories for some scripts you can use to bring up a cluster managed by Rancher in Dimension Data's cloud.
* A server for storage  
  This server will need to export an NFS volume unless you have your own options for storage
* A DNS `A` record pointing to your cluster  
  The record should resolve to the IP addresses of all nodes in your clusters, round-robin style.

### Deployment

#### Images

* `docker build -t sql-executor-image-name:sql-executor-image-tag -f .\Dockerfile.sql-executor .`
* `docker push sql-executor-image-name:sql-executor-image-tag`
* `docker build -t api-image-name:api-image-tag -f .\Dockerfile.api .`
* `docker push api-image-name:api-image-tag`
* `docker build -t provisioning-image-name:provisioning-image-tag -f .\Dockerfile.provisioning .`
* `docker push provisioning-image-name:provisioning-image-tag`

#### Kubernetes Resources

Customise the files in [deploy/k8s](deploy/k8s) as required, and run `kubectl create -Rf deploy/kubernetes`.
