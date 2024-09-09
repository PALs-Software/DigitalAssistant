#include "espeak_phonemize_wrapper.h"
#include "pch.h"
#include "piper-phonemize/phonemize.hpp"
#include "espeak-ng/speak_lib.h"
#include "piper-phonemize/uni_algo.h"

std::string LAST_ERROR_MESSAGE = "";
PhonemeSentences RESULT;
std::map<std::string, piper::PhonemeMap> DEFAULT_PHONEME_MAP = {
	{"pt-br", {{U'c', {U'k'}}}} };

int InitWrapper(const char* eSpeak_data_path)
{
	std::vector<piper::Phoneme>* sentencePhonemes = nullptr;
	
	return espeak_Initialize(AUDIO_OUTPUT_SYNCHRONOUS, 0, eSpeak_data_path, 0);
}

// Copied from https://github.com/rhasspy/piper-phonemize/blob/master/src/phonemize.cpp
void PhonemizeESpeakWrapper(std::string text, piper::eSpeakPhonemeConfig& config, std::vector<std::vector<piper::Phoneme>>& phonemes)
{
	auto voice = config.voice;
	int result = espeak_SetVoiceByName(voice.c_str());
	if (result != 0) {
		throw "Failed to set eSpeak-ng voice";
	}

	std::shared_ptr<piper::PhonemeMap> phonemeMap;
	if (config.phonemeMap) {
		phonemeMap = config.phonemeMap;
	}
	else if (DEFAULT_PHONEME_MAP.count(voice) > 0) {
		phonemeMap = std::make_shared<piper::PhonemeMap>(DEFAULT_PHONEME_MAP[voice]);
	}

	// Modified by eSpeak
	std::string textCopy(text);

	std::vector<piper::Phoneme>* sentencePhonemes = nullptr;
	const char* inputTextPointer = textCopy.c_str();
	int terminator = 0;

	while (inputTextPointer != NULL) {
		// Modified espeak-ng API to get access to clause terminator
		std::string clausePhonemes(espeak_TextToPhonemesWithTerminator(
			(const void**)&inputTextPointer,
			/*textmode*/ espeakCHARS_AUTO,
			/*phonememode = IPA*/ 0x02, &terminator));

		// Decompose, e.g. "ç" -> "c" + "̧"
		auto phonemesNorm = una::norm::to_nfd_utf8(clausePhonemes);
		auto phonemesRange = una::ranges::utf8_view{ phonemesNorm };

		if (!sentencePhonemes) {
			// Start new sentence
			phonemes.emplace_back();
			sentencePhonemes = &phonemes[phonemes.size() - 1];
		}

		// Maybe use phoneme map
		std::vector<piper::Phoneme> mappedSentPhonemes;
		if (phonemeMap) {
			for (auto phoneme : phonemesRange) {
				if (phonemeMap->count(phoneme) < 1) {
					// No mapping for phoneme
					mappedSentPhonemes.push_back(phoneme);
				}
				else {
					// Mapping for phoneme
					auto mappedPhonemes = &(phonemeMap->at(phoneme));
					mappedSentPhonemes.insert(mappedSentPhonemes.end(),
						mappedPhonemes->begin(),
						mappedPhonemes->end());
				}
			}
		}
		else {
			// No phoneme map
			mappedSentPhonemes.insert(mappedSentPhonemes.end(), phonemesRange.begin(),
				phonemesRange.end());
		}

		auto phonemeIter = mappedSentPhonemes.begin();
		auto phonemeEnd = mappedSentPhonemes.end();

		if (config.keepLanguageFlags) {
			// No phoneme filter
			sentencePhonemes->insert(sentencePhonemes->end(), phonemeIter,
				phonemeEnd);
		}
		else {
			// Filter out (lang) switch (flags).
			// These surround words from languages other than the current voice.
			bool inLanguageFlag = false;

			while (phonemeIter != phonemeEnd) {
				if (inLanguageFlag) {
					if (*phonemeIter == U')') {
						// End of (lang) switch
						inLanguageFlag = false;
					}
				}
				else if (*phonemeIter == U'(') {
					// Start of (lang) switch
					inLanguageFlag = true;
				}
				else {
					sentencePhonemes->push_back(*phonemeIter);
				}

				phonemeIter++;
			}
		}

		// Add appropriate punctuation depending on terminator type
		int punctuation = terminator & 0x000FFFFF;
		if (punctuation == CLAUSE_PERIOD) {
			sentencePhonemes->push_back(config.period);
		}
		else if (punctuation == CLAUSE_QUESTION) {
			sentencePhonemes->push_back(config.question);
		}
		else if (punctuation == CLAUSE_EXCLAMATION) {
			sentencePhonemes->push_back(config.exclamation);
		}
		else if (punctuation == CLAUSE_COMMA) {
			sentencePhonemes->push_back(config.comma);
			sentencePhonemes->push_back(config.space);
		}
		else if (punctuation == CLAUSE_COLON) {
			sentencePhonemes->push_back(config.colon);
			sentencePhonemes->push_back(config.space);
		}
		else if (punctuation == CLAUSE_SEMICOLON) {
			sentencePhonemes->push_back(config.semicolon);
			sentencePhonemes->push_back(config.space);
		}

		if ((terminator & CLAUSE_TYPE_SENTENCE) == CLAUSE_TYPE_SENTENCE) {
			// End of sentence
			sentencePhonemes = nullptr;
		}

	} // while inputTextPointer != NULL

}

void ResetResultStructure()
{
	RESULT.Success = false;
	LAST_ERROR_MESSAGE = "";
	for (size_t i = 0; i < RESULT.SentencesCount; i++)
		delete[] RESULT.Sentences[i].Phonemes;

	delete[] RESULT.Sentences;
	RESULT.SentencesCount = 0;
}

PhonemeSentences ConvertTextToPhonemesWrapper(const char* text, const char* voice)
{
	ResetResultStructure();
	std::string str_text(text);
	std::string str_voice(voice);
	std::vector<std::vector<piper::Phoneme>> sentences;
	piper::eSpeakPhonemeConfig eSpeak_config;
	eSpeak_config.voice = str_voice;

	try
	{
		PhonemizeESpeakWrapper(str_text, eSpeak_config, sentences);
		RESULT.SentencesCount = static_cast<int>(sentences.size());
		RESULT.Sentences = new PhonemeSentence[RESULT.SentencesCount];

		for (int i = 0; i < RESULT.SentencesCount; i++)
		{
			RESULT.Sentences[i].PhonemesCount = static_cast<int>(sentences[i].size());
			RESULT.Sentences[i].Phonemes = new char32_t[RESULT.Sentences[i].PhonemesCount];
			for (size_t y = 0; y < RESULT.Sentences[i].PhonemesCount; y++)
				RESULT.Sentences[i].Phonemes[y] = sentences[i][y];
		}

		RESULT.Success = true;
	}
	catch (const std::exception& ex)
	{
		RESULT.Success = false;
		LAST_ERROR_MESSAGE = std::string(ex.what());
		RESULT.ErrorMessage = (char*)LAST_ERROR_MESSAGE.data();
	}

	return RESULT;
}