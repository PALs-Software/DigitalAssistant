#pragma once

#include <Arduino.h>
#include <cstdint>
#include <cstring>
#include <string>
#include <array>
#include <vector>
#include "tcp_message_type.h"
#include "modules/guid.h"

class TcpMessage
{
public:
    static constexpr int MESSAGE_HEADER_BYTE_LENGTH = 35;

    TcpMessage(TcpMessageType message_type, guid_t event_id_guid, byte *data_array, int64_t message_length)
    {
        type = message_type;
        std::memcpy(event_id, event_id_guid, sizeof(guid_t));
        data = data_array;
        data_length = message_length;
    }

    TcpMessageType GetType();
    void GetEventId(guid_t id);
    byte *GetData();
    int64_t GetDataLength();

    void GetMessageHeaderBytes(byte* message_header_bytes);

    static TcpMessage *CreateNewMessageFromHeaderData(byte *message_header_data);

private:
    static constexpr char MESSAGE_START_TOKEN[] = "|<SOM>|";

    TcpMessageType type;
    guid_t event_id;
    byte *data;
    int64_t data_length;
};