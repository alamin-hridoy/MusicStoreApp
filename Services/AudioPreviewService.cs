using System.Text;
using MusicStoreApp.Utilities;

namespace MusicStoreApp.Services;

public sealed class AudioPreviewService
{
    private const int SampleRate = 22_050;
    private const int PreviewSeconds = 14;

    public byte[] Generate(string locale, string seed, int index)
    {
        var random = new StableRandom(StableRandom.Compose(StableRandom.ComposeString(locale, seed), (ulong)index, 0xA0D10UL));
        var arrangement = Arrangement.Create(random);
        var sampleCount = SampleRate * PreviewSeconds;
        var samples = new short[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var time = i / (double)SampleRate;
            var beat = time * arrangement.BeatsPerSecond;
            var bar = (int)(beat / 4.0);
            var step = (int)(beat * 2.0);

            var chord = arrangement.Progression[bar % arrangement.Progression.Length];
            var melodyMidi = arrangement.MelodyPattern[step % arrangement.MelodyPattern.Length];
            var leadFrequency = MidiToFrequency(melodyMidi);
            var chordRoot = chord[0];
            var bassFrequency = MidiToFrequency(chordRoot - 12 - arrangement.BassOctaveDrop);
            var padFrequencyA = MidiToFrequency(chord[1]);
            var padFrequencyB = MidiToFrequency(chord[2]);

            var leadGate = StepEnvelope(beat, arrangement.LeadGate, arrangement.LeadDecay);
            var bassGate = StepEnvelope(beat, arrangement.BassGate, arrangement.BassDecay);
            var hatPulse = Pulse(beat, arrangement.HatDensity, arrangement.HatWidth) * arrangement.HatLevel;
            var kickPulse = Pulse(beat, 1.0, 0.16) * arrangement.KickLevel;
            var snarePulse = Pulse(beat - 0.5, 1.0, 0.18) * arrangement.SnareLevel;

            var lead = LeadVoice(time, leadFrequency, arrangement, leadGate);
            var pad = PadVoice(time, padFrequencyA, padFrequencyB, arrangement);
            var bass = BassVoice(time, bassFrequency, arrangement, bassGate);
            var kick = KickVoice(time, beat, arrangement) * kickPulse;
            var snare = NoiseVoice(time, beat, arrangement.SnareDecay, 1900, arrangement.SnareTone) * snarePulse;
            var hat = NoiseVoice(time, beat * arrangement.HatDensity, arrangement.HatDecay, 5200, 0.25) * hatPulse;

            var mixed = (lead + pad + bass + kick + snare + hat) * arrangement.MasterGain;
            samples[i] = (short)Math.Clamp(mixed * short.MaxValue, short.MinValue, short.MaxValue);
        }

        return WriteWav(samples, SampleRate);
    }

    private static double LeadVoice(double time, double frequency, Arrangement arrangement, double gate)
    {
        var sine = Math.Sin(2 * Math.PI * frequency * time);
        var triangle = 2 * Math.Asin(Math.Sin(2 * Math.PI * frequency * time)) / Math.PI;
        var shimmer = Math.Sin(2 * Math.PI * frequency * 2.01 * time) * arrangement.LeadShimmer;
        var vibrato = 1 + arrangement.VibratoDepth * Math.Sin(2 * Math.PI * arrangement.VibratoRate * time);
        return (sine * arrangement.LeadSineMix + triangle * arrangement.LeadTriangleMix + shimmer) * arrangement.LeadGain * gate * vibrato;
    }

    private static double PadVoice(double time, double frequencyA, double frequencyB, Arrangement arrangement)
    {
        var slowLfo = 0.72 + 0.28 * Math.Sin(2 * Math.PI * arrangement.PadLfoRate * time);
        var a = Math.Sin(2 * Math.PI * frequencyA * 0.5 * time);
        var b = Math.Sin(2 * Math.PI * frequencyB * 0.5 * time);
        return (a + b) * 0.5 * arrangement.PadGain * slowLfo;
    }

    private static double BassVoice(double time, double frequency, Arrangement arrangement, double gate)
    {
        var square = Math.Sign(Math.Sin(2 * Math.PI * frequency * time));
        var sub = Math.Sin(2 * Math.PI * frequency * 0.5 * time);
        return (square * arrangement.BassSquareMix + sub * arrangement.BassSubMix) * arrangement.BassGain * gate;
    }

    private static double KickVoice(double time, double beat, Arrangement arrangement)
    {
        var local = FractionalPart(beat);
        var pitch = 45 + 28 * Math.Exp(-local / 0.08);
        return Math.Sin(2 * Math.PI * pitch * time) * Math.Exp(-local / arrangement.KickDecay) * 0.8;
    }

    private static double NoiseVoice(double time, double beat, double decay, double cutoff, double tone)
    {
        var local = FractionalPart(beat);
        var noise = HashNoise(time * cutoff) * tone + Math.Sin(2 * Math.PI * cutoff * time) * (1 - tone);
        return noise * Math.Exp(-local / decay);
    }

    private static double StepEnvelope(double beat, IReadOnlyList<int> pattern, double decay)
    {
        var stepPosition = beat * 2.0;
        var stepIndex = (int)Math.Floor(stepPosition);
        var local = stepPosition - stepIndex;
        return pattern[stepIndex % pattern.Count] == 0 ? 0.0 : Math.Exp(-local / decay);
    }

    private static double Pulse(double beat, double density, double width)
    {
        var local = FractionalPart(beat * density);
        return local < width ? 1.0 - (local / width) : 0.0;
    }

    private static double FractionalPart(double value)
    {
        return value - Math.Floor(value);
    }

    private static double HashNoise(double value)
    {
        return (Math.Sin(value * 12.9898) * 43758.5453) % 1.0;
    }

    private static double MidiToFrequency(int midiNote)
    {
        return 440.0 * Math.Pow(2, (midiNote - 69) / 12.0);
    }

    private static byte[] WriteWav(short[] samples, int sampleRate)
    {
        var dataLength = samples.Length * sizeof(short);
        using var stream = new MemoryStream(44 + dataLength);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private sealed class Arrangement
    {
        public required double BeatsPerSecond { get; init; }
        public required int[][] Progression { get; init; }
        public required int[] MelodyPattern { get; init; }
        public required int BassOctaveDrop { get; init; }
        public required double LeadGain { get; init; }
        public required double LeadSineMix { get; init; }
        public required double LeadTriangleMix { get; init; }
        public required double LeadShimmer { get; init; }
        public required double VibratoDepth { get; init; }
        public required double VibratoRate { get; init; }
        public required double PadGain { get; init; }
        public required double PadLfoRate { get; init; }
        public required double BassGain { get; init; }
        public required double BassSquareMix { get; init; }
        public required double BassSubMix { get; init; }
        public required IReadOnlyList<int> LeadGate { get; init; }
        public required IReadOnlyList<int> BassGate { get; init; }
        public required double LeadDecay { get; init; }
        public required double BassDecay { get; init; }
        public required double KickLevel { get; init; }
        public required double KickDecay { get; init; }
        public required double SnareLevel { get; init; }
        public required double SnareDecay { get; init; }
        public required double SnareTone { get; init; }
        public required double HatLevel { get; init; }
        public required double HatDensity { get; init; }
        public required double HatWidth { get; init; }
        public required double HatDecay { get; init; }
        public required double MasterGain { get; init; }

        public static Arrangement Create(StableRandom random)
        {
            var tempos = new[] { 1.55, 1.7, 1.85, 2.0, 2.15, 2.3 };
            var scales = new[]
            {
                new[] { 0, 2, 4, 5, 7, 9, 11 },
                new[] { 0, 2, 3, 5, 7, 8, 10 },
                new[] { 0, 3, 5, 7, 10 }
            };
            var progressions = new[]
            {
                new[] { 0, 4, 5, 3 },
                new[] { 0, 5, 3, 4 },
                new[] { 0, 3, 5, 4 },
                new[] { 0, 6, 4, 5 }
            };
            var leadPatterns = new[]
            {
                new[] { 0, 2, 4, 2, 5, 4, 2, 0 },
                new[] { 0, 3, 4, 3, 2, 1, 2, 4 },
                new[] { 4, 3, 2, 0, 2, 4, 5, 4 },
                new[] { 0, 2, 0, 4, 3, 2, 5, 4 }
            };
            var leadGates = new[]
            {
                new[] { 1, 1, 1, 0, 1, 1, 0, 1 },
                new[] { 1, 0, 1, 1, 1, 0, 1, 1 },
                new[] { 1, 1, 0, 1, 1, 1, 0, 0 },
                new[] { 1, 0, 1, 0, 1, 1, 1, 1 }
            };
            var bassGates = new[]
            {
                new[] { 1, 0, 1, 0, 1, 0, 1, 0 },
                new[] { 1, 1, 0, 1, 1, 0, 1, 0 },
                new[] { 1, 0, 1, 1, 1, 0, 1, 1 }
            };

            var root = random.Next(46, 62);
            var scale = random.Pick(scales);
            var progressionTemplate = random.Pick(progressions);
            var leadTemplate = random.Pick(leadPatterns);
            var progression = progressionTemplate
                .Select(degree =>
                {
                    var normalizedDegree = degree % scale.Length;
                    var baseNote = root + scale[normalizedDegree];
                    var isMinorish = scale.Contains(3) || scale.Contains(10);
                    return new[]
                    {
                        baseNote,
                        baseNote + (isMinorish && normalizedDegree is 0 or 3 or 4 ? 3 : 4),
                        baseNote + 7,
                        baseNote + 12
                    };
                })
                .ToArray();

            var melodyPattern = Enumerable.Range(0, PreviewSeconds * 2)
                .Select(step =>
                {
                    var chord = progression[(step / 4) % progression.Length];
                    var leadDegree = leadTemplate[step % leadTemplate.Length] % chord.Length;
                    return chord[leadDegree] + (random.NextBool(0.22) ? 12 : 0);
                })
                .ToArray();

            return new Arrangement
            {
                BeatsPerSecond = random.Pick(tempos),
                Progression = progression,
                MelodyPattern = melodyPattern,
                BassOctaveDrop = random.Next(0, 2),
                LeadGain = 0.18 + random.NextDouble() * 0.08,
                LeadSineMix = 0.35 + random.NextDouble() * 0.35,
                LeadTriangleMix = 0.25 + random.NextDouble() * 0.4,
                LeadShimmer = 0.02 + random.NextDouble() * 0.08,
                VibratoDepth = 0.006 + random.NextDouble() * 0.018,
                VibratoRate = 3.5 + random.NextDouble() * 3.5,
                PadGain = 0.08 + random.NextDouble() * 0.09,
                PadLfoRate = 0.08 + random.NextDouble() * 0.24,
                BassGain = 0.17 + random.NextDouble() * 0.09,
                BassSquareMix = 0.34 + random.NextDouble() * 0.34,
                BassSubMix = 0.25 + random.NextDouble() * 0.35,
                LeadGate = random.Pick(leadGates),
                BassGate = random.Pick(bassGates),
                LeadDecay = 0.22 + random.NextDouble() * 0.22,
                BassDecay = 0.28 + random.NextDouble() * 0.24,
                KickLevel = 0.16 + random.NextDouble() * 0.12,
                KickDecay = 0.08 + random.NextDouble() * 0.07,
                SnareLevel = 0.05 + random.NextDouble() * 0.08,
                SnareDecay = 0.05 + random.NextDouble() * 0.05,
                SnareTone = 0.55 + random.NextDouble() * 0.35,
                HatLevel = 0.02 + random.NextDouble() * 0.05,
                HatDensity = random.NextBool(0.5) ? 2.0 : 4.0,
                HatWidth = 0.08 + random.NextDouble() * 0.06,
                HatDecay = 0.015 + random.NextDouble() * 0.03,
                MasterGain = 0.44
            };
        }
    }
}
