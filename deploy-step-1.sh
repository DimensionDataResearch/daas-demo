#!/bin/bash

set -euo pipefail

BASEDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

pushd $BASEDIR/terraform/ddcloud

terraform apply
terraform refresh

popd # $BASEDIR/terraform/ddcloud

pushd $BASEDIR/ansible

ansible all -m raw -a 'apt-get update && apt-get install -y python'
ansible-playbook playbooks/reboot-servers.yml
ansible all -m copy -a 'src=./roles/expand-root-volume/files/fdisk-script dest=/root/fdisk-script'
ansible all -m shell -a 'fdisk /dev/sda < /root/fdisk-script'
ansible all -a 'partprobe /dev/sda'
ansible all -a 'pvcreate /dev/sda3'
ansible all -a 'vgextend ubuntu-cloud-vg /dev/sda3'
ansible all -a 'lvextend -l +100%FREE /dev/ubuntu-cloud-vg/ubuntu-main'
ansible all -a 'resize2fs /dev/ubuntu-cloud-vg/ubuntu-main'
ansible-playbook playbooks/upgrade-packages.yml
ansible-playbook playbooks/reboot-servers.yml

ansible-playbook kubernetes-on-rancher.yml

popd # $BASEDIR/ansible
