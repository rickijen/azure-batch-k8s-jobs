#!/bin/bash

# Author: Ganesh Radhakrishnan @Microsoft
# Dated: 07-23-2020
# Description:
# This script deploys a single node Kubernetes cluster.
# Run this script from a 'Start Task' in Azure Batch.

echo "**k8s-install*** Begin *****"

# Start by sleeping for 1 min, let Azure finish running OS updates
WAIT_SECS=60
echo "**k8s-install*** Pausing [$WAIT_SECS] seconds to let Azure finish running OS updates *****"
sleep $WAIT_SECS

# Let the script run even if there are errors
set -e

echo "***k8s-install*** Current working directory [$PWD] *****"
echo "***k8s-install*** AZ_BATCH_NODE_STARTUP_DIR [$AZ_BATCH_NODE_STARTUP_DIR] *****"
echo "***k8s-install*** AZ_BATCH_TASK_DIR [$AZ_BATCH_TASK_DIR] *****"
echo "***k8s-install*** AZ_BATCH_TASK_WORKING_DIR [$AZ_BATCH_TASK_WORKING_DIR] *****"

# Install k8s tools
curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo apt-key add -

cat <<EOF | sudo tee /etc/apt/sources.list.d/kubernetes.list
deb https://apt.kubernetes.io/ kubernetes-xenial main
EOF

sudo apt-get update
sudo apt-get install -y kubelet kubeadm kubectl
sudo apt-mark hold kubelet kubeadm kubectl
echo "**k8s-install*** Installed kubelet, kubeadm and kubectl *****"

# Initialize kubeadm
ecode=0
sudo kubeadm init || ecode=$?
echo "**k8s-install*** kubeadm init finished. Exit status=[$ecode] *****"

# Copy kubeconfig file to startup task working directory
sudo cp -i /etc/kubernetes/admin.conf $AZ_BATCH_NODE_STARTUP_DIR/config
sudo chown labuser:labuser $AZ_BATCH_NODE_STARTUP_DIR/config
sudo chmod 444 $AZ_BATCH_NODE_STARTUP_DIR/config
echo "**k8s-install*** Copied kube config to task home [$AZ_BATCH_NODE_STARTUP_DIR] directory *****"

# Create alias for issuing kubectl commands from user's home directory
kuberun="kubectl --kubeconfig=$AZ_BATCH_NODE_STARTUP_DIR/config"

# Deploy a pod network to the cluster
$kuberun apply -f https://docs.projectcalico.org/v3.14/manifests/calico.yaml
echo "**k8s-install*** Deployed Calico pod overlay network *****"

# Pause $WAIT_SECS seconds for k8s networking and coredns pods to boot up!
echo "**k8s-install*** Pausing [$WAIT_SECS] seconds to let calico initialize k8s network *****"
sleep $WAIT_SECS

# List all pods
$kuberun get pods --all-namespaces

# Un-taint the node to allow user pods to run
$kuberun taint nodes --all node-role.kubernetes.io/master-

echo "**k8s-install*** Finished *****"
