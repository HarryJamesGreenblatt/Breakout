using Godot;
using System;
using System.Collections.Generic;
using Breakout.Models;

namespace Breakout.Components
{
    /// <summary>
    /// SoundComponent — generates 8-bit style sounds using square wave synthesis + cracking noise.
    /// 
    /// Following Nystrom's Component pattern:
    /// - Owns all audio generation and playback responsibility
    /// - Listens to game events and plays appropriate sounds
    /// - Supports polyphonic playback (multiple sounds simultaneously)
    /// 
    /// Architecture:
    /// - Generates AudioStreamWav objects in memory with PCM data
    /// - Creates square wave samples for tones (arcade authentic)
    /// - Generates cracking noise bursts layered over brick hits
    /// - Uses ADSR-like envelope for natural sound shaping
    /// - Plays via multiple AudioStreamPlayers (Godot 4.5 documented API)
    /// </summary>
    public partial class SoundComponent : Node
    {
        #region Audio Configuration
        private const int SAMPLE_RATE = 44100;
        private const float MASTER_VOLUME = 0.3f;
        #endregion

        #region State
        private AudioStreamPlayer audioPlayer;
        private List<AudioStreamPlayer> crackPlayers;  // Pool for polyphonic cracking sounds
        private Dictionary<string, SoundPreset> soundPresets;
        #endregion

        #region Sound Presets
        /// <summary>
        /// Defines a sound effect by frequency, duration, envelope, and waveform characteristics.
        /// Arcade Breakout sounds use square waves with pitch sweeps for that classic harsh/buzzy tone.
        /// </summary>
        private class SoundPreset
        {
            public float frequency;           // Hz (starting pitch)
            public float duration;            // seconds
            public float attackTime;          // seconds (fade-in)
            public float decayTime;           // seconds (fade-out)
            public float maxAmplitude;        // 0.0 to 1.0 (volume)
            public WaveformType waveform;     // sine, square, noise
            public float frequencySweep;      // Hz/sec (pitch glide: negative = down, positive = up)

            public SoundPreset(float freq, float dur, float attack, float decay, float amplitude, 
                WaveformType wave = WaveformType.Square, float sweep = 0f)
            {
                frequency = freq;
                duration = dur;
                attackTime = attack;
                decayTime = decay;
                maxAmplitude = amplitude;
                waveform = wave;
                frequencySweep = sweep;
            }
        }

        private enum WaveformType
        {
            Sine,
            Square,
            Noise
        }
        #endregion

        #region Lifecycle
        public override void _Ready()
        {
            // Create and configure main audio player
            audioPlayer = new AudioStreamPlayer();
            AddChild(audioPlayer);

            // Create pool of crack players for polyphonic cracking (one per note)
            crackPlayers = new List<AudioStreamPlayer>();
            for (int i = 0; i < 10; i++)  // Pool of 10 simultaneous crack sounds
            {
                var player = new AudioStreamPlayer();
                AddChild(player);
                crackPlayers.Add(player);
            }

            InitializeSoundPresets();

            GD.Print("SoundComponent initialized (synthesized 8-bit audio with polyphonic cracking)");
        }

        private void InitializeSoundPresets()
        {
            soundPresets = new Dictionary<string, SoundPreset>()
            {
                // Arcade Breakout uses square waves (harsh/buzzy) with pitch sweeps for character
                { "PaddleHit", new SoundPreset(800f, 0.12f, 0.01f, 0.08f, 0.5f, WaveformType.Square, -200f) },      // Square down-sweep
                { "BrickHit", new SoundPreset(1200f, 0.1f, 0.008f, 0.06f, 0.45f, WaveformType.Square, -150f) },      // Higher square down-sweep
                { "BrickDestroyed", new SoundPreset(1600f, 0.18f, 0.01f, 0.10f, 0.55f, WaveformType.Square, -400f) }, // High square pitch drop
                { "SpeedIncrease", new SoundPreset(1400f, 0.15f, 0.01f, 0.08f, 0.5f, WaveformType.Square, 100f) },    // Square up-sweep
                { "PaddleShrink", new SoundPreset(600f, 0.22f, 0.015f, 0.12f, 0.45f, WaveformType.Square, -300f) },   // Lower square down-sweep
                { "PaddleShrinkEffect", new SoundPreset(500f, 0.08f, 0.005f, 0.05f, 0.5f, WaveformType.Square, -200f) }, // Short gaw-like tone
                { "LivesDecremented", new SoundPreset(400f, 0.35f, 0.05f, 0.25f, 0.5f, WaveformType.Square, -600f) },  // Low ominous down-sweep
                { "GameOver", new SoundPreset(300f, 0.55f, 0.05f, 0.35f, 0.55f, WaveformType.Square, -800f) },         // Very low defeat tone
                { "WallBounce", new SoundPreset(1000f, 0.08f, 0.005f, 0.05f, 0.35f, WaveformType.Square, 50f) },       // Quick pop with slight rise
                // Cracking noise: harsh noise burst for polyphonic layering
                { "Crack", new SoundPreset(0f, 0.035f, 0.001f, 0.025f, 0.4f, WaveformType.Noise, 0f) }                // Very brief noise burst
            };
        }
        #endregion

        #region Public API - Play Sounds
        /// <summary>
        /// Plays a sound effect immediately.
        /// Called by game event handlers.
        /// </summary>
        public void PlayPaddleHit() => PlaySound(soundPresets["PaddleHit"]);
        
        /// <summary>
        /// Plays brick hit sound with polyphonic cracking layered over the tone.
        /// The number of crack repetitions scales by brick color (higher value = more cracks).
        /// Yellow(bottom) = 1 crack, Green = 2, Orange = 3, Red(top) = 4 cracks
        /// </summary>
        public void PlayBrickHit(BrickColor color = BrickColor.Yellow)
        {
            // Play the base brick tone
            PlaySound(soundPresets["BrickHit"]);

            // Get crack count based on brick color (value = proximity to top)
            int crackCount = color switch
            {
                BrickColor.Red => 4,      // Top = most cracks (highest point value)
                BrickColor.Orange => 3,
                BrickColor.Green => 2,
                BrickColor.Yellow => 1,   // Bottom = 1 crack (lowest point value)
                _ => 1
            };

            // Play staggered cracking noises polyphonically
            PlayPolyphonicCracks(crackCount);
        }

        public void PlayBrickDestroyed() => PlaySound(soundPresets["BrickDestroyed"]);
        public void PlaySpeedIncrease() => PlaySound(soundPresets["SpeedIncrease"]);
        public void PlayPaddleShrink() => PlaySound(soundPresets["PaddleShrink"]);
        public void PlayPaddleShrinkEffect()
        {
            // Play "gaw gaw gaw" effect: 3 rapid descending tones
            SoundPreset shrinkPreset = soundPresets["PaddleShrinkEffect"];
            float delayBetweenGaws = 0.12f;  // 120ms between each "gaw"

            for (int i = 0; i < 3; i++)
            {
                float delay = i * delayBetweenGaws;
                ScheduleShrinkGaw(shrinkPreset, i, delay);
            }
        }

        public void PlayLivesDecremented() => PlaySound(soundPresets["LivesDecremented"]);
        public void PlayGameOver() => PlaySound(soundPresets["GameOver"]);
        public void PlayWallBounce() => PlaySound(soundPresets["WallBounce"]);
        #endregion

        #region Private - Audio Synthesis (Godot 4.5 API)
        /// <summary>
        /// Plays a synthesized sound by:
        /// 1. Generating PCM samples from square wave synthesis
        /// 2. Creating an AudioStreamWav from the samples
        /// 3. Assigning it to AudioStreamPlayer and playing it
        /// 
        /// Uses documented Godot 4.5 C# API for AudioStreamWav.
        /// </summary>
        private void PlaySound(SoundPreset preset)
        {
            try
            {
                // Create AudioStreamWav from the preset
                AudioStreamWav stream = CreateAudioStreamWav(preset);

                // Assign to player and play
                audioPlayer.Stream = stream;
                audioPlayer.Play();

                GD.Print($"Playing: {preset.frequency}Hz for {preset.duration}s");
            }
            catch (System.Exception e)
            {
                GD.PrintErr($"Error playing sound: {e.Message}");
            }
        }

        /// <summary>
        /// Creates an AudioStreamWav from a SoundPreset.
        /// 
        /// Godot 4.5 AudioStreamWav API (documented):
        /// - Data property: PackedByteArray of PCM samples
        /// - Format property: FORMAT_16_BITS for signed 16-bit PCM
        /// - MixRate property: Sample rate (44100 Hz standard)
        /// - Stereo property: false for mono audio
        /// </summary>
        private AudioStreamWav CreateAudioStreamWav(SoundPreset preset)
        {
            // Calculate total samples needed
            int totalSamples = (int)(SAMPLE_RATE * preset.duration);

            // Generate PCM data as byte array (16-bit signed, little-endian)
            byte[] pcmData = new byte[totalSamples * 2];

            for (int i = 0; i < totalSamples; i++)
            {
                float timeInSeconds = i / (float)SAMPLE_RATE;
                float sample = GenerateSample(timeInSeconds, preset);

                // Convert float (-1 to 1) to 16-bit signed integer
                short sampleInt16 = (short)(sample * 32767f);

                // Store as little-endian bytes
                pcmData[i * 2] = (byte)(sampleInt16 & 0xFF);
                pcmData[i * 2 + 1] = (byte)((sampleInt16 >> 8) & 0xFF);
            }

            // Create and configure AudioStreamWav
            var stream = new AudioStreamWav();
            stream.Data = pcmData;
            stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
            stream.MixRate = SAMPLE_RATE;
            stream.Stereo = false;

            return stream;
        }

        /// <summary>
        /// Generates a single audio sample at time t using arcade-authentic synthesis.
        /// Supports square wave with optional pitch sweep and envelope shaping.
        /// </summary>
        private float GenerateSample(float timeInSeconds, SoundPreset preset)
        {
            // Apply frequency sweep: frequency changes over time
            float currentFreq = preset.frequency + (preset.frequencySweep * timeInSeconds);
            currentFreq = Mathf.Max(20f, currentFreq);  // Clamp to audible range

            // Generate waveform based on type
            float baseWave = preset.waveform switch
            {
                WaveformType.Square => GenerateSquareWave(currentFreq, timeInSeconds),
                WaveformType.Noise => GenerateNoise(),
                _ => GenerateSineWave(currentFreq, timeInSeconds)
            };

            // Calculate envelope (attack and decay shaping)
            float envelope = 1.0f;

            // Attack phase: ramp up from 0 to 1 over attackTime
            if (timeInSeconds < preset.attackTime)
            {
                envelope = timeInSeconds / preset.attackTime;
            }
            // Decay phase: ramp down from 1 to 0 over decayTime at end of sound
            else if (timeInSeconds > (preset.duration - preset.decayTime))
            {
                float timeIntoDecay = timeInSeconds - (preset.duration - preset.decayTime);
                envelope = 1.0f - (timeIntoDecay / preset.decayTime);
            }

            // Clamp envelope to 0-1 range
            envelope = Mathf.Clamp(envelope, 0.0f, 1.0f);

            // Combine: wave * envelope * volume
            float sample = baseWave * envelope * preset.maxAmplitude * MASTER_VOLUME;

            return sample;
        }

        private float GenerateSineWave(float frequency, float timeInSeconds)
        {
            return Mathf.Sin(2 * Mathf.Pi * frequency * timeInSeconds);
        }

        private float GenerateSquareWave(float frequency, float timeInSeconds)
        {
            // Classic square wave: simple phase comparison
            float phase = (frequency * timeInSeconds) % 1.0f;
            return phase < 0.5f ? 1.0f : -1.0f;
        }

        private float GenerateNoise()
        {
            // Simple pseudo-random noise using C# Random
            // For arcade authenticity, this would be very subtle
            return (float)(GD.Randf() * 2.0f - 1.0f);
        }

        /// <summary>
        /// Plays multiple cracking sounds polyphonically (simultaneously).
        /// Each crack is played using a separate player from the pool.
        /// </summary>
        private void PlayPolyphonicCracks(int crackCount)
        {
            SoundPreset crackPreset = soundPresets["Crack"];
            float delayBetweenCracks = 0.05f;  // 50ms delay between each crack to build compounding effect

            for (int i = 0; i < crackCount && i < crackPlayers.Count; i++)
            {
                try
                {
                    // Schedule each crack with increasing delay
                    // Yellow (1): 0ms
                    // Green (2): 0ms, 50ms
                    // Orange (3): 0ms, 50ms, 100ms
                    // Red (4): 0ms, 50ms, 100ms, 150ms
                    float delay = i * delayBetweenCracks;
                    
                    ScheduleCrackPlay(crackPreset, i, delay);

                    GD.Print($"Playing crack {i + 1}/{crackCount}");
                }
                catch (System.Exception e)
                {
                    GD.PrintErr($"Error playing crack sound: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Schedules a crack sound to play after a delay (enables compounding effect).
        /// </summary>
        private void ScheduleCrackPlay(SoundPreset crackPreset, int crackIndex, float delaySeconds)
        {
            // Use Godot's CreateTimer to schedule playback after a delay
            GetTree().CreateTimer(delaySeconds).Timeout += () => {
                try
                {
                    // Create audio stream for this crack
                    AudioStreamWav stream = CreateAudioStreamWav(crackPreset);

                    // Get player from pool based on index
                    int playerIndex = crackIndex % crackPlayers.Count;
                    var player = crackPlayers[playerIndex];

                    // Assign and play
                    player.Stream = stream;
                    player.Play();
                }
                catch (System.Exception e)
                {
                    GD.PrintErr($"Error scheduling crack play: {e.Message}");
                }
            };
        }

        /// <summary>
        /// Schedules a shrink "gaw" sound to play after a delay (enables "gaw gaw gaw" effect).
        /// </summary>
        private void ScheduleShrinkGaw(SoundPreset shrinkPreset, int gawIndex, float delaySeconds)
        {
            // Use Godot's CreateTimer to schedule playback after a delay
            GetTree().CreateTimer(delaySeconds).Timeout += () => {
                try
                {
                    // Create audio stream for this gaw
                    AudioStreamWav stream = CreateAudioStreamWav(shrinkPreset);

                    // Get player from pool based on index
                    int playerIndex = (gawIndex + 5) % crackPlayers.Count;  // Use different pool slots than cracks
                    var player = crackPlayers[playerIndex];

                    // Assign and play
                    player.Stream = stream;
                    player.Play();
                }
                catch (System.Exception e)
                {
                    GD.PrintErr($"Error scheduling gaw play: {e.Message}");
                }
            };
        }
        #endregion
    }
}
