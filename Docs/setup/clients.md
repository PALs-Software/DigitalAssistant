# Clients
The client is the connection between the user and the digital assistant. It receives the user's requests and returns the corresponding answers via some connected loudspeakers. Also multiple clients can be connected to the server, for example to use the digital assistant in several rooms.

## Connect new client with the server

### Auto discovered clients
1. Select the **Clients** tab in the administration sidebar tab.
2. Choose the action **Add new client** to add a new client.
3. A dropdown list in the popup will show all discovered clients which can be connected to the server. If no client is discovered you can also add your client [manually](#manually).
4. Select the client you want to connect to the server from the drop-down list.
5. Press the action **Add selected client**.

### Manually
1. Select the **Clients** tab in the administration sidebar tab.
2. Choose the action **Add new client** to add a new client.
3. Press the action **Add a new client manually**.
4. Fill in the necessary fields.
   - Name
   - Type
5. Press the action **Save**.
6. A message provide now a temporary access token. 
7. Copy the access token to the `appsettings.json` file of the client application in the `ServerAccessToken` property.
8. Add the host name or IP address of the server in the `ServerName` property of the `appsettings.json` of the client.
9. The next time the client application starts, it automatically connects to the server.

## Configure Client
1. Select the **Clients** tab in the administration sidebar tab.
2. Open the card of the client to be configured via the edit button.
3. Specify the changes in the fields of the card, like the input and output devices or the default volume after startup.
4. Press the **Save** action.