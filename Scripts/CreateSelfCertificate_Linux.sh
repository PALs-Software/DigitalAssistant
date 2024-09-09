#! /bin/bash
read -p "Enter the user name which will execute the website later: " serviceUser
read -p "Enter the website address (in the most cases the hostname or localhost): " websiteAddress

sudo apt-get install openssl
mkdir -p /etc/ssl/digitalassistant

PARENT="$websiteAddress"

openssl req \
-x509 \
-newkey rsa:4096 \
-sha256 \
-days 3650 \
-nodes \
-keyout /etc/ssl/digitalassistant/ssl.key \
-out /etc/ssl/digitalassistant/ssl.crt \
-subj "/CN=${PARENT}" \
-extensions v3_ca \
-extensions v3_req \
-config <( \
  echo '[req]'; \
  echo 'default_bits= 4096'; \
  echo 'distinguished_name=req'; \
  echo 'x509_extension = v3_ca'; \
  echo 'req_extensions = v3_req'; \
  echo '[v3_req]'; \
  echo 'basicConstraints = CA:FALSE'; \
  echo 'keyUsage = nonRepudiation, digitalSignature, keyEncipherment'; \
  echo 'subjectAltName = @alt_names'; \
  echo '[ alt_names ]'; \
  echo "DNS.1 = www.${PARENT}"; \
  echo "DNS.2 = ${PARENT}"; \
  echo '[ v3_ca ]'; \
  echo 'subjectKeyIdentifier=hash'; \
  echo 'authorityKeyIdentifier=keyid:always,issuer'; \
  echo 'basicConstraints = critical, CA:TRUE, pathlen:0'; \
  echo 'keyUsage = critical, cRLSign, keyCertSign'; \
  echo 'extendedKeyUsage = serverAuth, clientAuth')

openssl x509 -noout -text -in /etc/ssl/digitalassistant/ssl.crt

sudo chown -R root:$serviceUser /etc/ssl/digitalassistant
sudo chmod 640 /etc/ssl/digitalassistant/ssl.key

sudo sudo cp /etc/ssl/digitalassistant/ssl.crt /usr/local/share/ca-certificates/digitalassistant-ssl.crt
sudo update-ca-certificates