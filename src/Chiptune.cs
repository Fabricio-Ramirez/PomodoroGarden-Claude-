using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Text;

namespace PomodoroGarden
{
    // Little 8-bit jingles, synthesised in memory at startup (no sound files needed).
    static class Chiptune
    {
        static SoundPlayer bloom, wake, pop;

        public static void Init()
        {
            try
            {
                bloom = Make(new[] { 72, 76, 79, 84, 79, 84 }, new[] { 0.08, 0.08, 0.08, 0.14, 0.08, 0.55 });
                wake = Make(new[] { 84, 79, 84, 79, 88 }, new[] { 0.1, 0.1, 0.1, 0.1, 0.5 });
                pop = Make(new[] { 79, 86 }, new[] { 0.04, 0.08 });
            }
            catch { } // no audio device: stay silent
        }

        public static void Bloom() { Play(bloom); }  // focus finished
        public static void Wake() { Play(wake); }    // break finished
        public static void Pop() { Play(pop); }      // timer started

        static void Play(SoundPlayer p)
        {
            if (p == null) return;
            try { p.Play(); } catch { }
        }

        // notes are MIDI numbers; each plays a 25% pulse wave plus a soft triangle an octave below.
        static SoundPlayer Make(int[] notes, double[] durations)
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
                    double v = (pulse * 0.6 + tri * 0.4) * attack * decay * release * 0.3;
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
            ms.Position = 0;

            var player = new SoundPlayer(ms);
            player.Load();
            return player;
        }
    }
}
