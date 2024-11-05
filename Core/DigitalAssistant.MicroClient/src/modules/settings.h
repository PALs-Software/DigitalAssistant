#pragma once
#include <Arduino.h>
#include <Preferences.h>

class Settings
{
public:
      static void Init(Preferences *storage);

      static bool GetIsConfigured();
      static void SetIsConfigured(bool is_configured);

      static String GetServerAddress();
      static void SetServerAddress(String server_address);

      static char* GetServerCertificate();
      static void SetServerCertificate(const char* server_certificate, size_t byte_length);

      static byte* GetAccessToken(size_t &byte_length);
      static void SetAccessToken(byte *access_token, size_t byte_length);

      static bool GetPlayRequestSound();
      static void SetPlayRequestSound(bool play_request_sound);

      static float GetVolume();
      static void SetVolume(float volume);

private:
      static Preferences *storage;
};
