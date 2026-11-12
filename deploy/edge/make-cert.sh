#!/bin/sh
# Makes the self-signed certificate edge-nginx answers port 443 with (certs/edge.crt and
# certs/edge.key next to this script). A load balancer in front that ends the public TLS
# usually does not check it. Without such a balancer, put a real certificate under the same
# names instead. Leaves an existing certificate alone unless called with --force.
set -eu
cd "$(dirname "$0")"
mkdir -p certs

if [ -f certs/edge.crt ] && [ "${1:-}" != "--force" ]; then
    echo "certs/edge.crt already exists, pass --force to replace it"
    exit 0
fi

openssl req -x509 -newkey rsa:2048 -nodes -days 3650 \
    -subj "/CN=edge" \
    -keyout certs/edge.key -out certs/edge.crt 2>/dev/null
# nginx reads the key as root before dropping privileges, but keep it out of other users' reach.
chmod 600 certs/edge.key
echo "certs/edge.crt and certs/edge.key written, valid for 10 years"
