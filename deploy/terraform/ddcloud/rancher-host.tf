# The Rancher
#
# Exports /dev/sdb over NFS for consumption by Kubernetes
#
resource "ddcloud_server" "rancher_host" {
    name                    = "rancher-host"
    description				= "Rancher server for Adam's ITaaS RancherLab environment."
	admin_password			= "${var.ssh_bootstrap_password}"
    power_state				= "autostart"

    networkdomain           = "${data.ddcloud_networkdomain.primary.id}"

    primary_network_adapter {
		vlan				= "${data.ddcloud_vlan.primary.id}"
		ipv4				= "${cidrhost(format("%s/%s", data.ddcloud_vlan.primary.ipv4_base_address, data.ddcloud_vlan.primary.ipv4_prefix_size), 10)}"
	}

	memory_gb				= 8
	cpu_count				= 2

	dns_primary				= "8.8.8.8"
	dns_secondary			= "8.8.4.4"

	disk {
		scsi_unit_id		= 0
		size_gb				= 40
		speed				= "STANDARD"
	}

	disk {
		scsi_unit_id		= 1
		size_gb				= 120
		speed				= "STANDARD"
	}

	image					= "${var.image}"

	tag {
		name                = "roles"
		value				= "rancher-host"
	}
}

output "rancher_host" {
    value = [
        "${ddcloud_server.rancher_host.name}"
    ]
}
output "rancher_host_ipv4_private" {
    value = [
        "${ddcloud_server.rancher_host.primary_adapter_ipv4}"
    ]
}
output "rancher_host_ipv4_public" {
    value = [
        "${ddcloud_nat.rancher_host.public_ipv4}"
    ]
}

# The host must be publicly accessible for provisioning.
resource "ddcloud_nat" "rancher_host" {
	networkdomain	= "${data.ddcloud_networkdomain.primary.id}"
	private_ipv4	= "${ddcloud_server.rancher_host.primary_adapter_ipv4}"
}

resource "cloudflare_record" "rancher_host" {
    domain  = "${var.domain_name}"
    name    = "${ddcloud_server.rancher_host.name}.${var.subdomain_name}"
    value   = "${ddcloud_nat.rancher_host.public_ipv4}"
    type    = "A"

    proxied = false
}

resource "ddcloud_address_list" "rancher_host" {
	name			= "RancherHosts"
	ip_version		= "IPv4"

	addresses		= [ "${ddcloud_nat.rancher_host.public_ipv4}" ]

	networkdomain	= "${data.ddcloud_networkdomain.primary.id}"
}

# Install an SSH key so that Ansible doesnt make us jump through hoops to authenticate.
resource "null_resource" "rancher_install_ssh_key" {
	# Install our SSH public key.
	provisioner "remote-exec" {
		inline = [
			"mkdir -p ~/.ssh",
			"chmod 700 ~/.ssh",
			"echo '${file(var.ssh_public_key_file)}' > ~/.ssh/authorized_keys",
			"chmod 600 ~/.ssh/authorized_keys",
			"passwd -d root"
		]

		connection {
			type 		= "ssh"
			
			user 		= "root"
			password 	= "${var.ssh_bootstrap_password}"

			host 		= "${ddcloud_nat.rancher_host.public_ipv4}"
		}
	}
}
