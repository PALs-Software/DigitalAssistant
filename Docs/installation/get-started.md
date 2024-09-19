# Getting Started

The Digital Assistant is divided into two components. On the one hand a server component and on the other a client component. The server and client can be installed on any device, your desktop PC, a server or a single-board computers such as the Raspberry Pi in different ways, for example as independent docker container. The server is used to process the requests from the client devices and manage the administrational things. The client is the connection between the user and the digital assistant. It receives the user's requests and returns the corresponding answers via some connected loudspeakers.

The simplest way to try out the Digital Assistant is to install the server component via a Docker container and try out the client functions via the website hosted with it. But a local installation of server and client on the same desktop PC is also possible.

Decide for yourself which of the installation paths you like best. You can find detailed instructions on how to install the server [here](host-server.md) and all information you need to install a client [here](host-client.md).

## Quickstart
As described above, the simplest form of the Digital Assistant is the server installation via Docker with the use of the integrated client in the web interface. To do this, carry out the following steps:

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
2. Use the integrated terminal in Docker Desktop or open another command terminal of your choice.
3. Execute the following command. This will download and run the digital assistant server application inside a docker container.
```
docker run --name digital-assistant-server -p 8079:8079 -p 8080:8080 -v DigitalAssistantServer_Data:/app/DigitalAssistantServer -v DigitalAssistantServer_Keys:/home/app/.aspnet/DataProtection-Keys -d palssoftware/digital-assistant-server

```
4. Open `https://localhost:8080/` in a browser and enjoy the digital assistant. Further information's how to configure and use the digital assistant can be found in the [setup](../setup/setup.md) chapter of this documentation.