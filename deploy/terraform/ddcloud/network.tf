data "ddcloud_networkdomain" "primary" {
    name        = "${var.target_networkdomain}"

    datacenter  = "${var.target_datacenter}"
}

data "ddcloud_vlan" "primary" {
    name            = "${var.target_vlan}"

    networkdomain   = "${data.ddcloud_networkdomain.primary.id}"
}

resource "ddcloud_address_list" "admin_clients_ipv4" {
    name       = "adminclients.ipv4"
    ip_version = "IPv4"

    networkdomain   = "${data.ddcloud_networkdomain.primary.id}"

    addresses = [
        "${var.admin_client_ips}"
    ]
}

resource "ddcloud_firewall_rule" "admin_clients_inbound" {
    name = "adminclients.inbound.ipv4.any"
    placement           = "first"
    action              = "accept"
    enabled             = true

    ip_version          = "ipv4"
    protocol            = "ip"

    source_address_list = "${ddcloud_address_list.admin_clients_ipv4.id}"

    networkdomain       = "${data.ddcloud_networkdomain.primary.id}"
}
