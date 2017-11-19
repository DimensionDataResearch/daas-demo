variable "target_region"            { default = "AU" }

# The name of the target datacenter.
variable "target_datacenter"        { default = "AU10" }

# The name of the target network domain (must already exist).
variable "target_networkdomain"     { }

# The name of the target VLAN (must already exist).
variable "target_vlan"              { }

# The number of worker hosts to deploy.
variable "host_count"               { default = 3 }

# The number of Kubernetes storage hosts to deploy.
variable "kube_storage_host_count"  { default = 4 }

# The number of storage hosts to deploy (leave this as 1 unless you're planning on deploying something like Gluster or Ceph).
variable "storage_host_count"       { default = 1 }

# The name of the image used to create the hosts.
# variable "image"                  { default = "Ubuntu 14.04 2 CPU" }
variable "image"                    { default = "Ubuntu 16.04 64-bit 2 CPU" }

variable "ssh_bootstrap_password"   { }
variable "ssh_public_key_file"      { default = "~/.ssh/id_rsa.pub" }
variable "admin_client_ips"         { type = "list" }

provider "ddcloud" {
    region = "${var.target_region}"
}

# DNS

variable "domain_name"            { }
variable "subdomain_name"         { }
variable "cloudflare_email"       { }
variable "cloudflare_token"       { }

provider "cloudflare" {
    email = "${var.cloudflare_email}"
    token = "${var.cloudflare_token}"
}
