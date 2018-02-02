# Kubernetes worker hosts
#
resource "ddcloud_server" "kube_host" {
    count                   = "${var.host_count}"
    
    name                    = "kube-host-${format("%02d", count.index + 1)}"
    description				= "Kubernetes host ${format("%02d", count.index + 1)}."
	admin_password			= "${var.ssh_bootstrap_password}"
    power_state				= "autostart"

    networkdomain           = "${data.ddcloud_networkdomain.primary.id}"

    primary_network_adapter {
		vlan				= "${data.ddcloud_vlan.primary.id}"
		ipv4				= "${cidrhost(format("%s/%s", data.ddcloud_vlan.primary.ipv4_base_address, data.ddcloud_vlan.primary.ipv4_prefix_size), 20 + count.index)}"
	}

	memory_gb				= 16
	cpu_count				= 4

	dns_primary				= "8.8.8.8"
	dns_secondary			= "8.8.4.4"

	disk {
		scsi_unit_id		= 0
		size_gb				= 120
		speed				= "STANDARD"
	}

	image					= "${var.image}"

	tag {
		name                = "roles"
		value				= "kube-host"
	}
}

output "kube_host" {
    value = [
        "${ddcloud_server.kube_host.*.name}"
    ]
}
output "kube_host_ipv4_private" {
    value = [
        "${ddcloud_server.kube_host.*.primary_adapter_ipv4}"
    ]
}
output "kube_host_ipv4_public" {
    value = [
        "${ddcloud_nat.kube_host.*.public_ipv4}"
    ]
}

# The host must be publicly accessible for provisioning.
resource "ddcloud_nat" "kube_host" {
	count			= "${var.host_count}"

	networkdomain	= "${data.ddcloud_networkdomain.primary.id}"
	private_ipv4	= "${element(ddcloud_server.kube_host.*.primary_adapter_ipv4, count.index)}"
}

resource "cloudflare_record" "kube_host" {
    count   = "${var.host_count}"

    domain  = "${var.domain_name}"
    name    = "${element(ddcloud_server.kube_host.*.name, count.index)}.${var.subdomain_name}"
    value   = "${element(ddcloud_nat.kube_host.*.public_ipv4, count.index)}"
    type    = "A"

    proxied = false
}

resource "cloudflare_record" "kube_host_wildcard" {
    count   = "${var.host_count}"

    domain  = "${var.domain_name}"
    name    = "*.${var.subdomain_name}"
    value   = "${element(ddcloud_nat.kube_host.*.public_ipv4, count.index)}"
    type    = "A"

    proxied = false
}

resource "ddcloud_address_list" "kube_hosts" {
	name			= "KubeHosts"
	ip_version		= "IPv4"

	addresses		= [ "${ddcloud_nat.kube_host.*.public_ipv4}" ]

	networkdomain	= "${data.ddcloud_networkdomain.primary.id}"
}

# Install an SSH key so that Ansible doesnt make us jump through hoops to authenticate.
resource "null_resource" "kube_install_ssh_key" {
	count = "${var.host_count}"

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

			host 		= "${element(ddcloud_nat.kube_host.*.public_ipv4, count.index)}"
		}
	}
}
