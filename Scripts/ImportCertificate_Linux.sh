#! /bin/bash
read -p "Enter the path to the .crt certificate file: " certificatePath

sudo sudo cp $certificatePath /usr/local/share/ca-certificates/digitalassistant-ssl.crt
sudo update-ca-certificates