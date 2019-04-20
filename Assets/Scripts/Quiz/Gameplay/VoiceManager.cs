using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Quiz.Gameplay
{
    public class VoiceManager
    {
        [DllImport("SAPI_UNITY_DLL")]
        private static extern int Uni_Voice_Init();

        [DllImport("SAPI_UNITY_DLL")]
        private static extern void Uni_Voice_Close();

        [DllImport("SAPI_UNITY_DLL")]
        private static extern int Uni_Voice_Status(int voiceStat);

        [DllImport("SAPI_UNITY_DLL")]
        private static extern int
            Uni_Voice_Speak([MarshalAs(UnmanagedType.LPWStr)] string textToSpeech); // SPF_ASYNC & SPF_IS_XML

        [DllImport("SAPI_UNITY_DLL")]
        private static extern int
            Uni_Voice_SpeakEX([MarshalAs(UnmanagedType.LPWStr)] string textToSpeech, int voiceFlag); // CUSTOM FLAG

        [DllImport("SAPI_UNITY_DLL")]
        private static extern int Uni_Voice_Volume(int volume); //  zero to 100

        [DllImport("SAPI_UNITY_DLL")]
        private static extern int Uni_Voice_Rate(int rate); // -10 to 10

        [DllImport("SAPI_UNITY_DLL")]
        private static extern void Uni_Voice_Pause();

        [DllImport("SAPI_UNITY_DLL")]
        private static extern void Uni_Voice_Resume();

//    SPF_DEFAULT
//        Specifies that the default settings should be used.  The defaults are:
//            * Speak the given text string synchronously
//            * Not purge pending speak requests
//            * Parse the text as XML only if the first character is a left-angle-bracket (<)
//            * Not persist global XML state changes across speak calls
//            * Not expand punctuation characters into words.
//        To override this default, use the other flag values given below.
//
//    SPF_ASYNC
//        Specifies that the Speak call should be asynchronous. That is, it will return immediately after the speak request is queued.
//    SPF_PURGEBEFORESPEAK
//        Purges all pending speak requests prior to this speak call.
//    SPF_IS_FILENAME
//        The string passed to Uni_Voice_Speak is a file name, and the file text should be spoken.
//    SPF_IS_XML
//        The input text will be parsed for XML markup.
//    SPF_IS_NOT_XML
//        The input text will not be parsed for XML markup.
//    SPF_PERSIST_XML
//        Global state changes in the XML markup will persist across speak calls.
//    SPF_NLP_SPEAK_PUNC
//        Punctuation characters should be expanded into words (e.g. "This is a sentence." would become "This is a sentence period").
//    SPF_PARSE_SAPI
//        Force XML parsing As MS SAPI.
//    SPF_PARSE_SSML
//        Force XML parsing As W3C SSML.

        const int SPF_DEFAULT = 0;
        const int SPF_ASYNC = 1;
        const int SPF_PURGEBEFORESPEAK = 2;
        const int SPF_IS_FILENAME = 4;
        const int SPF_IS_XML = 8;
        const int SPF_IS_NOT_XML = 16;
        const int SPF_PERSIST_XML = 32;
        const int SPF_NLP_SPEAK_PUNC = 64;
        const int SPF_PARSE_SAPI = 128;
        const int SPF_PARSE_SSML = 256;

        private int _voiceNumber;
        private string[] _voiceNames;

        public VoiceManager()
        {
            // Info (64Bits OS):
            // Executing Windows\sysWOW64\speech\SpeechUX\SAPI.cpl brings up a Window that displays (!) all of the 32 bit Voices
            // and the current single 64 bit Voice "Anna".

            // HKEY_LOCAL_MACHINE\\SOFTWARE\Microsoft\Speech\\Voices\\Tokens\\xxxVOICExxxINSTALLEDxxx\\Attributes >>> (Name)
            const string speechTokens = "Software\\Microsoft\\Speech\\Voices\\Tokens";

            using (var registryKey = Registry.LocalMachine.OpenSubKey(speechTokens))
            {
                if (registryKey == null)
                    return;

                _voiceNumber = registryKey.SubKeyCount; // key found not mean true voice number !!!
                _voiceNames = new string[_voiceNumber + 1];
                _voiceNumber = 0;

                foreach (var regKeyFound in registryKey.GetSubKeyNames())
                {
                    var finalKey = "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Speech\\Voices\\Tokens\\" + regKeyFound +
                                   "\\Attributes";

                    var gotchaVoiceName = (string) Registry.GetValue(finalKey, "Name", "");

                    if (string.IsNullOrEmpty(gotchaVoiceName))
                        continue;

                    _voiceNumber++;
                    _voiceNames[_voiceNumber] = gotchaVoiceName;
                }
            }

            if (_voiceNumber != 0)
                Uni_Voice_Init();
        }


        public int Say(string textToSay)
        {
            // https://msdn.microsoft.com/en-us/library/ms717077(v=vs.85).aspx

            // "This is <emph>very</emph> important!  This sounds normal <pitch middle = '+40'/> but the pitch drops half way through"
            // "This is <emph>very</emph> important!. This is very important! "
            // "<volume level="Level">  Text to be spoken</volume>"
            // "<rate speed="-5">This text should be spoken at rate zero.</rate>" -10 to +10
            // "<silence msec='1000'/>"
            // "I will spell the word <spell>Love</spell> for you."
            // "<voice required='Name=Microsoft Anna'> Hello i'm Anna. Have a nice day. <voice required='Name=IVONA 2 Amy'> It's Amy here. Good day too"
            // "All system <emph>ready</emph>. Engine online, weapons online. We are ready. 9<silence msec='1000'/>8<silence msec='1000'/>7<silence msec='1000'/>6<silence msec='1000'/>5<silence msec='1000'/>4<silence msec='1000'/>3<silence msec='1000'/>2<silence msec='1000'/>1<silence msec='1000'/>0. Take off!!!"
            return Uni_Voice_Speak(textToSay);
        }

        public int SayEX(string textToSay, int flagOfSpeak)
        {
            return Uni_Voice_SpeakEX(textToSay, flagOfSpeak);
        }

        public int Status(int statToCheck)
        {
            // statToCheck = 0 >> return '2' for a running speak. '0' or '1' in other case.
            // statToCheck = 1 >> return the position of the actual spoken word in the textToSay string. ;)
            //        ex for "Hello my friend" can return the position of H >> 1, m >> 7 and f >> 10
            // statToCheck = 2 >> return Total speak stream
            // statToCheck = 3 >> return Actual speak stream
            return Uni_Voice_Status(statToCheck);
        }

        public void Volume(int vol)
        {
            // 0 to 100
            Uni_Voice_Volume(vol);
        }

        public void Rate(int rat)
        {
            // -10 to 10
            Uni_Voice_Rate(rat);
        }

        public void Pause()
        {
            Uni_Voice_Pause();
        }

        public void Resume()
        {
            Uni_Voice_Resume();
        }

        public void Close()
        {
            Uni_Voice_Close();
        }
    }
}