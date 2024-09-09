SHELL=/bin/bash

all:
	g++ -fpic -shared espeak_phonemize_wrapper.cpp -lespeak-ng -lpiper_phonemize -I./Libs/piper-phonemize/linux_aarch64/include -L./Libs/piper-phonemize/linux_aarch64/lib -o ./../../Core/DigitalAssistant.Server/bin/Debug/net8.0/PhonemizeESpeakWrapper.so -Wl,-rpath,'$$ORIGIN'
	cp ./Libs/piper-phonemize/linux_aarch64/lib/* ./../../Core/DigitalAssistant.Server/bin/Debug/net8.0/

export_aarch64:
	mkdir -p ./Export
	g++ -fpic -shared espeak_phonemize_wrapper.cpp -lespeak-ng -lpiper_phonemize -I./Libs/piper-phonemize/linux_aarch64/include -L./Libs/piper-phonemize/linux_aarch64/lib -o ./Export/PhonemizeESpeakWrapper.so -Wl,-rpath,'$$ORIGIN'
	cp ./Libs/piper-phonemize/linux_aarch64/lib/* ./Export/

export_armv7:
	mkdir -p ./Export
	g++ -fpic -shared espeak_phonemize_wrapper.cpp -lespeak-ng -lpiper_phonemize -I./Libs/piper-phonemize/linux_armv7/include -L./Libs/piper-phonemize/linux_armv7/lib -o ./Export/PhonemizeESpeakWrapper.so -Wl,-rpath,'$$ORIGIN'
	cp ./Libs/piper-phonemize/linux_armv7/lib/* ./Export/

export_x86_64:
	mkdir -p ./Export
	g++ -fpic -shared espeak_phonemize_wrapper.cpp -lespeak-ng -lpiper_phonemize -I./Libs/piper-phonemize/linux_x86_64/include -L./Libs/piper-phonemize/linux_x86_64/lib -o ./Export/PhonemizeESpeakWrapper.so -Wl,-rpath,'$$ORIGIN'
	cp ./Libs/piper-phonemize/linux_x86_64/lib/* ./Export/