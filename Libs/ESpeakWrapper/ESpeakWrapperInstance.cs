using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ESpeakWrapper;

public partial class ESpeakWrapperInstance
{
    #region Consts
    private const string LIBRARY_NAME = "espeak-ng";

    private const int CLAUSE_INTONATION_FULL_STOP = 0x00000000;
    private const int CLAUSE_INTONATION_COMMA = 0x00001000;
    private const int CLAUSE_INTONATION_QUESTION = 0x00002000;
    private const int CLAUSE_INTONATION_EXCLAMATION = 0x00003000;

    private const int CLAUSE_TYPE_CLAUSE = 0x00040000;
    private const int CLAUSE_TYPE_SENTENCE = 0x00080000;

    private const int CLAUSE_PERIOD = (40 | CLAUSE_INTONATION_FULL_STOP | CLAUSE_TYPE_SENTENCE);
    private const int CLAUSE_COMMA = (20 | CLAUSE_INTONATION_COMMA | CLAUSE_TYPE_CLAUSE);
    private const int CLAUSE_QUESTION = (40 | CLAUSE_INTONATION_QUESTION | CLAUSE_TYPE_SENTENCE);
    private const int CLAUSE_EXCLAMATION = (45 | CLAUSE_INTONATION_EXCLAMATION | CLAUSE_TYPE_SENTENCE);
    private const int CLAUSE_COLON = (30 | CLAUSE_INTONATION_FULL_STOP | CLAUSE_TYPE_CLAUSE);
    private const int CLAUSE_SEMICOLON = (30 | CLAUSE_INTONATION_COMMA | CLAUSE_TYPE_CLAUSE);
    #endregion

    #region Imports
    [LibraryImport(LIBRARY_NAME, EntryPoint = "espeak_Initialize", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int NativeInitialize(int output, int bufferLength, string path, int options);

    [LibraryImport(LIBRARY_NAME, EntryPoint = "espeak_SetVoiceByName", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int NativeSetVoiceByName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [LibraryImport(LIBRARY_NAME, EntryPoint = "espeak_TextToPhonemesWithTerminator", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr NativeTextToPhonemesWithTerminator(ref IntPtr textptr, int textmode, int phonememode, out int terminator);
    #endregion

    #region Members
    private static bool DllResolverSet = false;
    #endregion

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "espeak-ng")
            return IntPtr.Zero;

        var executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        ArgumentNullException.ThrowIfNull(executionDirectory);
        var baseDirectory = Path.Combine(executionDirectory, "espeak-ng-libraries");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return NativeLibrary.Load(Path.Combine(baseDirectory, "windows", "espeak-ng.dll"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X86 or Architecture.X64 => NativeLibrary.Load(Path.Combine(baseDirectory, "linux_x86_64", "libespeak-ng.so")),
                Architecture.Armv6 or Architecture.Arm => NativeLibrary.Load(Path.Combine(baseDirectory, "linux_armv7", "libespeak-ng.so")),
                Architecture.LoongArch64 or Architecture.Arm64 => NativeLibrary.Load(Path.Combine(baseDirectory, "linux_aarch64", "libespeak-ng.so")),
                _ => IntPtr.Zero
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => NativeLibrary.Load(Path.Combine(baseDirectory, "mac_x64", "libespeak-ng.dylib")),
                Architecture.LoongArch64 or Architecture.Arm64 => NativeLibrary.Load(Path.Combine(baseDirectory, "mac_aarch64", "libespeak-ng.dylib")),
                _ => IntPtr.Zero
            };
        }

        return IntPtr.Zero;
    }

    public static int Initialize(string path)
    {
        if (!DllResolverSet)
        {
            NativeLibrary.SetDllImportResolver(typeof(ESpeakWrapperInstance).Assembly, DllImportResolver);
            DllResolverSet = true;
        }

        return NativeInitialize(2, 0, path, 0);
    }

    public static int SetVoiceByName(string voice)
    {
        return NativeSetVoiceByName(voice);
    }

    public static void ConvertTextToPhonemes(string text, out bool success, out string? result, out string? error)
    {
        success = true;
        result = String.Empty;
        error = null;

        IntPtr textPtr = Marshal.StringToHGlobalAnsi(text);
        try
        {
            while (textPtr != IntPtr.Zero)
            {
                IntPtr resultPtr = NativeTextToPhonemesWithTerminator(ref textPtr, 0, 0x02, out var terminator);
                result += Marshal.PtrToStringUTF8(resultPtr);

                int punctuation = terminator & 0x000FFFFF;
                switch (punctuation)
                {
                    case CLAUSE_PERIOD:
                        result += textPtr == IntPtr.Zero ? "." : ". ";
                        break;
                    case CLAUSE_QUESTION:
                        result += textPtr == IntPtr.Zero ? "?" : "? ";
                        break;
                    case CLAUSE_EXCLAMATION:
                        result += textPtr == IntPtr.Zero ? "!" : "! ";
                        break;
                    case CLAUSE_COMMA:
                        result += ", ";
                        break;
                    case CLAUSE_COLON:
                        result += ": ";
                        break;
                    case CLAUSE_SEMICOLON:
                        result += "; ";
                        break;
                }
            }
        }
        catch (Exception e)
        {
            success = false;
            error = e.Message;
        }
        finally
        {
            Marshal.FreeHGlobal(textPtr);
        }
    }

}
