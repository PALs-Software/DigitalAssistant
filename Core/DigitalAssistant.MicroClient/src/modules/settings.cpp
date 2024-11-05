#include "settings.h"

Preferences *Settings::storage;

void Settings::Init(Preferences *storage_pointer)
{
    storage = storage_pointer;

    storage->begin("Settings", false); // Must be called in or after the setup method of main, not in the constructor before that
}

bool Settings::GetIsConfigured()
{
    return storage->getBool("is_configured");
}

void Settings::SetIsConfigured(bool is_configured)
{
    storage->putBool("is_configured", is_configured);
}

String Settings::GetServerAddress()
{
    return storage->getString("server_address");
}

void Settings::SetServerAddress(String server_address)
{
    storage->putString("server_address", server_address);
}

char* Settings::GetServerCertificate()
{
    size_t byte_length = storage->getBytesLength("certificate");
    char *server_certificate = new char[byte_length];
    storage->getBytes("certificate", server_certificate, byte_length);

    return server_certificate;
}

void Settings::SetServerCertificate(const char* server_certificate, size_t byte_length)
{
    storage->putBytes("certificate", server_certificate, byte_length);
}

byte *Settings::GetAccessToken(size_t &byte_length)
{
    byte_length = storage->getBytesLength("access_token");
    byte *access_token = new byte[byte_length];
    storage->getBytes("access_token", access_token, byte_length);

    return access_token;
}

void Settings::SetAccessToken(byte *access_token, size_t byte_length)
{
    storage->putBytes("access_token", access_token, byte_length);
}

bool Settings::GetPlayRequestSound()
{
    return storage->getBool("play_req_sound");
}

void Settings::SetPlayRequestSound(bool play_request_sound)
{
    storage->putBool("play_req_sound", play_request_sound);
}

float Settings::GetVolume()
{
    return storage->getFloat("volume");
}

void Settings::SetVolume(float volume)
{
    storage->putFloat("volume", volume);
}