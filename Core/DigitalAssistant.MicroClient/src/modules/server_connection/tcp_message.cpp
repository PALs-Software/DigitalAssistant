#include "tcp_message.h"

constexpr char TcpMessage::MESSAGE_START_TOKEN[];

TcpMessageType TcpMessage::GetType()
{
    return type;
}

void TcpMessage::GetEventId(guid_t id)
{
    std::memcpy(id, event_id, sizeof(guid_t));
}

byte *TcpMessage::GetData()
{
    return data;
}

int64_t TcpMessage::GetDataLength()
{
    return data_length;
}

void TcpMessage::GetMessageHeaderBytes(byte* message_header_bytes)
{
    byte *message_header_bytes_pointer = message_header_bytes;

    std::memcpy(message_header_bytes_pointer, MESSAGE_START_TOKEN, 7);
    message_header_bytes_pointer += 7;

    int32_t type_int = static_cast<int32_t>(type);
    std::memcpy(message_header_bytes_pointer, &type_int, 4);
    message_header_bytes_pointer += 4;

    std::memcpy(message_header_bytes_pointer, event_id, 16);
    message_header_bytes_pointer += 16;

    std::memcpy(message_header_bytes_pointer, &data_length, 8);
}

TcpMessage *TcpMessage::CreateNewMessageFromHeaderData(byte *message_header_data)
{
    int32_t type_int;
    guid_t event_id;
    int64_t data_length;

    std::memcpy(&type_int, &message_header_data[7], 4);
    TcpMessageType message_type = static_cast<TcpMessageType>(type_int);
    std::memcpy(event_id, &message_header_data[11], 16);
    std::memcpy(&data_length, &message_header_data[27], 8);

    return new TcpMessage(message_type, event_id, (byte *)ps_malloc(data_length), data_length);
}
