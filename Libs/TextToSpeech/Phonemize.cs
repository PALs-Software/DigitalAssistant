using System.Runtime.InteropServices;
using System.Text;

namespace TextToSpeech;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PhonemeSentence
{
    public int PhonemesCount;
    public byte* Phonemes;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PhonemeSentences
{
    public bool Success;
    public IntPtr ErrorMessage;
    public int SentencesCount { get; set; }
    public PhonemeSentence* Sentences { get; set; }
};

public class Phonemize
{
    [DllImport("PhonemizeESpeakWrapper")]
    public static extern int InitWrapper(string eSpeak_data_path);

    [DllImport("PhonemizeESpeakWrapper")]
    private static extern PhonemeSentences ConvertTextToPhonemesWrapper(string text, string voice);


    public static unsafe List<string?> ConvertTextToPhonemes(string text, string voice)
    {
        var result = new List<string?>();
        var phonemeConvertResult = ConvertTextToPhonemesWrapper(text, voice);

        if (phonemeConvertResult.Success == false)
        {
            var errorMessage = Marshal.PtrToStringAnsi(phonemeConvertResult.ErrorMessage);
            throw new Exception(errorMessage);
        }

        for (int i = 0; i < phonemeConvertResult.SentencesCount; i++)
            result.Add(Encoding.UTF32.GetString(phonemeConvertResult.Sentences[i].Phonemes, phonemeConvertResult.Sentences[i].PhonemesCount * 4));

        return result;
    }
}
