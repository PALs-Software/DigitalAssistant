#pragma once

#ifndef linux

#ifdef PhonemizeESpeakWrapper_EXPORTS
#define LIBRARY_API __declspec(dllexport)
#else
#define LIBRARY_API __declspec(dllimport)
#endif

#else
#define LIBRARY_API
#endif

struct PhonemeSentence
{
    int PhonemesCount;
    char32_t* Phonemes;
};

struct PhonemeSentences
{
    bool Success;
    char* ErrorMessage;
    int SentencesCount;
    PhonemeSentence* Sentences;
};

extern "C" LIBRARY_API int InitWrapper(const char* eSpeak_data_path);

extern "C" LIBRARY_API PhonemeSentences ConvertTextToPhonemesWrapper(const char* text, const char* voice);