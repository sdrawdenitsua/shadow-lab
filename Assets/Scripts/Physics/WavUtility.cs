// WavUtility.cs — converts raw PCM bytes to Unity AudioClip
// Used by NovaVoice for Google Cloud TTS (LINEAR16 format)
using UnityEngine;

namespace ShadowLab.Physics
{
    public static class WavUtility
    {
        public static AudioClip ToAudioClip(byte[] wavBytes, string name)
        {
            if (wavBytes == null || wavBytes.Length < 44) return null;

            int channels   = wavBytes[22] | (wavBytes[23] << 8);
            int sampleRate = wavBytes[24] | (wavBytes[25] << 8) | (wavBytes[26] << 16) | (wavBytes[27] << 24);
            int bitDepth   = wavBytes[34] | (wavBytes[35] << 8);
            int dataStart  = 44; // standard WAV header

            int bytesPerSample = bitDepth / 8;
            int sampleCount    = (wavBytes.Length - dataStart) / bytesPerSample;

            float[] samples = new float[sampleCount];

            if (bitDepth == 16)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    int offset = dataStart + i * 2;
                    short raw  = (short)(wavBytes[offset] | (wavBytes[offset + 1] << 8));
                    samples[i] = raw / 32768f;
                }
            }

            AudioClip clip = AudioClip.Create(name, sampleCount / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
