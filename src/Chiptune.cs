using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;

namespace PomodoroGarden
{
    // Little 8-bit jingles, synthesised in memory at startup (no sound files needed).
    // They're played straight from pinned memory with the Windows PlaySound call: the
    // buffer can't move or be re-read while Windows is still playing it asynchronously.
    static class Chiptune
    {
        static Sound bloom, wake, pop;

        // What was played, most recent last (checked by the tests; no audio needed).
        public static readonly List<string> Log = new List<string>();
        public static bool LastPlayWorked; // did Windows accept the last sound

        [DllImport("winmm.dll", SetLastError = true)]
        static extern bool PlaySound(IntPtr sound, IntPtr module, uint flags);
        const uint SND_ASYNC = 0x1, SND_NODEFAULT = 0x2, SND_MEMORY = 0x4;

        sealed class Sound
        {
            public readonly string Name;
            public readonly byte[] Wav;
            readonly GCHandle pin;

            public Sound(string name, byte[] wav)
            {
                Name = name;
                Wav = wav;
                pin = GCHandle.Alloc(wav, GCHandleType.Pinned); // kept for the life of the app
            }

            public IntPtr Address { get { return pin.AddrOfPinnedObject(); } }
        }

        public static void Init()
        {
            try
            {
                bloom = new Sound("bloom", Wav(new[] { 72, 76, 79, 84, 79, 84 }, new[] { 0.08, 0.08, 0.08, 0.14, 0.08, 0.55 }));
                wake = new Sound("wake", Wav(new[] { 84, 79, 84, 79, 88 }, new[] { 0.1, 0.1, 0.1, 0.1, 0.5 }));
                pop = new Sound("pop", Wav(new[] { 79, 86 }, new[] { 0.05, 0.1 }));
            }
            catch { }
        }

        public static void Bloom() { Play(bloom, SystemSounds.Asterisk); }   // focus finished
        public static void Wake() { Play(wake, SystemSounds.Asterisk); }     // break finished
        public static void Pop() { Play(pop, null); }                        // timer started

        public static byte[] BloomWav { get { return bloom == null ? null : bloom.Wav; } }

        static void Play(Sound s, SystemSound fallback)
        {
            if (s == null) return;
            Log.Add(s.Name);
            bool ok = false;
            try { ok = PlaySound(s.Address, IntPtr.Zero, SND_MEMORY | SND_ASYNC | SND_NODEFAULT); }
            catch { } // no winmm (not Windows)
            LastPlayWorked = ok;
            if (!ok && fallback != null)
            {
                try { fallback.Play(); } catch { } // at least the Windows chime
            }
        }

        // notes are MIDI numbers; each plays a 25% pulse wave plus a soft triangle an octave below.
        public static byte[] Wav(int[] notes, double[] durations)
        {
            const int Rate = 22050;
            var samples = new List<short>();
            for (int n = 0; n < notes.Length; n++)
            {
                int count = (int)(durations[n] * Rate);
                double freq = 440.0 * Math.Pow(2, (notes[n] - 69) / 12.0);
                bool last = n == notes.Length - 1;
                for (int i = 0; i < count; i++)
                {
                    double t = i / (double)Rate;
                    double attack = Math.Min(1, t / 0.004);
                    double decay = last ? Math.Exp(-3.5 * t / durations[n]) : 1 - 0.35 * t / durations[n];
                    double release = Math.Min(1, (count - i) / (Rate * 0.006));
                    double pulse = (t * freq) % 1.0 < 0.25 ? 0.75 : -0.25;
                    double tri = 4 * Math.Abs((t * freq / 2) % 1.0 - 0.5) - 1;
                    double v = (pulse * 0.6 + tri * 0.4) * attack * decay * release * 0.45;
                    samples.Add((short)(v * 32767));
                }
            }

            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            int dataLen = samples.Count * 2;
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + dataLen); w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data")); w.Write(dataLen);
            foreach (short s in samples) w.Write(s);
            w.Flush();
            return ms.ToArray();
        }
    }
}
