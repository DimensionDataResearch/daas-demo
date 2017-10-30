variable "target_region"          { default = "AU" }

# The name of the target datacenter.
variable "target_datacenter"      { default = "AU9" }

# The name of the target network domain (must already exist).
variable "target_networkdomain"   { }

# The name of the target VLAN (must already exist).
variable "target_vlan"            { }

# The number of worker hosts to deploy.
variable "host_count"             { default = 3 }

# The number of storage hosts to deploy (leave this as 1 unless you're planning on deploying something like Gluster or Ceph).
variable "storage_host_count"     { default = 1 }

variable "ssh_bootstrap_password" { }
variable "ssh_public_key_file"    { default = "~/.ssh/id_rsa.pub" }
variable "admin_client_ips"       { type = "list" }

provider "ddcloud" {
    region = "${var.target_region}"
}