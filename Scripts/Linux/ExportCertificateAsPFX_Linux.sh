#! /bin/bash
openssl pkcs12 -export -out /etc/ssl/digitalassistant/ssl.pfx -inkey /etc/ssl/digitalassistant/ssl.key -in /etc/ssl/digitalassistant/ssl.crt