#!/bin/bash

helm install --name daas-consul stable/consul --set ui.enabled=true,uiService.enabled=true,Storage=500Mi,StorageClass=daas-data
