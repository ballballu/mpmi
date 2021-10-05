using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Events;
using MEC;
using System.Runtime.InteropServices;
using System.Threading;
#if UNITY_ANDROID && UNITY_OBOE
using Oboe.Stream;
#endif

namespace MidiPlayerTK
{
    /// <summary>
    /// [MPTK PRO] - class extention pro
    /// </summary>
    public partial class MidiSynth : MonoBehaviour
    {
        /// <summary>
        /// Delegate for the event OnAudioFrameStartHandler. see #OnAudioFrameStart
        /// </summary>
        /// <param name="synthTime"></param>
        public delegate void OnAudioFrameStartHandler(double synthTime);

        /// <summary>
        /// [MPTK PRO] - this event is triggered at each start of a new audio frame from the audio engine.<br>
        /// The parameter (double) is the current synth time in milliseconds. See example of use.\n
        /// The callbach function will not run on the Unity thread, so you can't call Unity API except Debug.Log.
        /// @code
        /// // See Assets\MidiPlayer\Demo\ProDemos\Script\EuclideSeq\TestEuclideanRhythme.cs for the full code.
        /// public void Play()
        /// {
        ///     if (IsPlaying)
        ///         midiStream.OnAudioFrameStart += PlayHits;
        ///     else
        ///         midiStream.OnAudioFrameStart -= PlayHits;
        /// }
        /// private void PlayHits(double synthTimeMS)
        /// {
        ///     if (lastSynthTime <= 0d)
        ///         // First call, init the last time
        ///         lastSynthTime = synthTimeMS;
        ///     // Calculate time in millisecond since the last loop
        ///     double deltaTime = synthTimeMS - lastSynthTime;
        ///     lastSynthTime = synthTimeMS;
        ///     timeMidiFromStartPlay += deltaTime;
        /// 
        ///     // Calculate time since last beat played
        ///     timeSinceLastBeat += deltaTime;
        /// 
        ///     // Slider SldTempo in BPM.
        ///     //  60 BPM means 60 beats in each minute, 1 beat per second, 1000 ms between beat.
        ///     // 120 BPM would be twice as fast: 120 beats in each minute, 2 per second, 500 ms between beat.
        ///     // Calculate the delay between two quarter notes in millisecond
        ///     CurrentTempo = (60d / SldTempo.Value) * 1000d;
        /// 
        ///     // Is it time to play a hit ?
        ///     if (IsPlaying && timeSinceLastBeat > CurrentTempo)
        ///     {
        ///         timeSinceLastBeat = 0d;
        ///         CurrentBeat++;
        ///     }
        /// }
        /// @endcode
        /// </summary>
        public event OnAudioFrameStartHandler OnAudioFrameStart;

        /// <summary>
        /// [MPTK PRO] V2.89.0 - This function is called by the MIDI sequencer before sending the MIDI message to the MIDI synthesizer.\n
        /// It can be used like a preprocessor of the MIDI events: it's possible to change the value of the MIDI events and therefore change the playback of the song.\n
        /// The callback function receives a MPTKEvent object by reference (normal, it's a C# class) also everything is possible: change note, velocity, channel, ..., even changing the MIDI type of the message!!! \n
        /// See below some examples of change.\n
        /// Note 1: the callback is running on a system thread not on the Unity thread. Unity API call is not possible except for the Debug.Log (to be gently used, it consumes CPU)\n
        /// Note 2: avoid heavy processing or waiting inside the callback otherwise MIDI playing accuracy will be bad.\n
        /// Note 3: the midiEvent is passed by reference to the callback, so re-instanciate object (midiEvent = new MPTKEvent()) or set to null, has no effect!\n
        /// Note 4: MIDI position attributs (Tick and RealTime) can be used in your algo but changing their values has no effect, it's too late!\n
        /// @code
        /// // See TestMidiFilePlayerScripting.cs for the demo.
        /// void Start()
        /// {
        ///     MidiFilePlayer midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
        ///     midiFilePlayer.OnMidiEvent = PreProcessMidi;
        /// }
        /// 
        /// // Some example 
        /// void PreProcessMidi(MPTKEvent midiEvent)
        /// {
        ///     switch (midiEvent.Command)
        ///     {
        ///         case MPTKCommand.NoteOn:
        ///             if (midiEvent.Channel != 9)
        ///                 // transpose 2 octaves
        ///                 midiEvent.Value += 24;
        ///             else
        ///                 // Drums are muted
        ///                 midiEvent.Velocity = 0;
        ///         break;
        ///         case MPTKCommand.PatchChange:
        ///             // Remove all patch change: all channels will played the default preset 0!!!
        ///             midiEvent.Command = MPTKCommand.MetaEvent;
        ///             midiEvent.Meta = MPTKMeta.TextEvent;
        ///             midiEvent.Info = "Patch Change removed";
        ///             break;
        ///     }
        /// }
        /// 
        /// @endcode
        /// </summary>
        public Action<MPTKEvent> OnMidiEvent;


        // ----------------------------------------------------------------------
        // Apply effect defined in SoundFont : apply individually on each voices
        // ----------------------------------------------------------------------

        /// <summary>
        /// [MPTK PRO] - Apply frequency low-pass filter as defined in the SoundFont.\n 
        /// This effect is processed with the fluidsynth algo independently on each voices but with a small decrease of performace (40%).
        /// </summary>
        [HideInInspector]
        public bool MPTK_ApplySFFilter;

        /// <summary>
        /// [MPTK PRO] - Frequency cutoff is defined in the SoundFont for each notes.\n
        /// This parameter increase or decrease the default SoundFont value. Range: -2000 to 3000
        /// </summary>
        [Range(-2000f, 3000f)]
        [HideInInspector]
        public float MPTK_SFFilterFreqOffset = 0f;

        /// <summary>
        /// [MPTK PRO] - Quality Factor is defined in the SoundFont for each notes.\n
        /// This parameter increase or decrease the default SoundFont value. Range: -96 to 96.
        /// </summary>
        [HideInInspector]
        public float MPTK_SFFilterQModOffset
        {
            get { return filterQModOffset; }
            set
            {
                if (filterQModOffset != value)
                {
                    filterQModOffset = Mathf.Clamp(value, -96f, 96f);
                    if (ActiveVoices != null)
                        foreach (fluid_voice voice in ActiveVoices)
                            if (voice.resonant_filter != null)
                                voice.resonant_filter.fluid_iir_filter_set_q(voice.q_dB, filterQModOffset);
                }
            }
        }

        [HideInInspector, SerializeField]
        private float filterQModOffset;

        /// <summary>
        /// [MPTK PRO] - Set Filter SoundFont default value as defined in fluidsynth.\n
        /// </summary>
        public void MPTK_SFFilterSetDefault()
        {
            MPTK_SFFilterFreqOffset = 0f;
            MPTK_SFFilterQModOffset = 0f;
        }

        [HideInInspector]
        /// <summary>
        /// [MPTK PRO] - Apply reverberation effect as defined in the SoundFont.\n
        /// This effect is processed with the fluidsynth algo independently on each voices but with a small decrease of performace (40%).
        /// </summary>
        public bool MPTK_ApplySFReverb;

        [HideInInspector]
        /// <summary>
        /// [MPTK PRO] - Reverberation level is defined in the SoundFont in the range [0, 1].\n
        /// This parameter is added to the the default SoundFont value.\n
        /// Range must be [-1, 1]
        /// </summary>
        [Range(-1f, 1f)]
        public float MPTK_SFReverbAmplify;

        [HideInInspector]
        /// <summary>
        /// [MPTK PRO] - Apply chorus effect as defined in the SoundFont.\n
        /// This effect is processed with the fluidsynth algo independently on each voices but with a small decrease of performace (10%).
        /// </summary>
        public bool MPTK_ApplySFChorus;

        [HideInInspector]
        /// <summary>
        /// [MPTK PRO] - Chorus level is defined in the SoundFont in the range [0, 1].\n
        /// This parameter is added to the the default SoundFont value.\n
        /// Range must be [-1, 1]
        /// </summary>
        [Range(-1f, 1f)]
        public float MPTK_SFChorusAmplify;

        fluid_revmodel reverb;
        private float[] fx_reverb;
        fluid_chorus chorus;
        private float[] fx_chorus;

        /// <summary>
        /// [MPTK PRO] - Set the SoundFont reverb effect room size. Controls concave reverb time between 0 (0.7 s) and 1 (12.5 s)\n
        /// V2.88.2\n
        /// </summary>
        [HideInInspector]
        public float MPTK_SFReverbRoomSize
        {
            get { return sfReverbRoomSize; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 1f);
                if (sfReverbRoomSize != newval)
                {
                    sfReverbRoomSize = newval;
                    SetParamSfReverb();
                }
            }
        }

        [HideInInspector, SerializeField]
        private float sfReverbRoomSize = 0.2f;


        /// <summary>
        /// [MPTK PRO] - Set the SoundFont reverb effect damp [0,1].\n
        /// Controls the reverb time frequency dependency. This controls the reverb time for the frequency sample rate/2\n
        /// When 0, the reverb time for high frequencies is the same as for DC frequency.\n
        /// When > 0, high frequencies have less reverb time than lower frequencies.\n
        /// V2.88.2\n
        /// </summary>
        [HideInInspector]
        public float MPTK_SFReverbDamp
        {
            get { return sfReverbDamp; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 1f);
                if (sfReverbDamp != newval)
                {
                    sfReverbDamp = newval;
                    SetParamSfReverb();
                }
            }
        }
        [HideInInspector, SerializeField]
        private float sfReverbDamp = 0f;

        /// <summary>
        /// [MPTK PRO] - Set the SoundFont reverb effect width [0,100].\n
        ///  Controls the left/right output separation.\n
        ///  When 0, there are no separation and the signal on left and right output is the same.This sounds like a monophonic signal.\n
        ///  When 100, the separation between left and right is maximum.\n
        /// V2.88.2\n
        /// </summary>
        [HideInInspector]
        public float MPTK_SFReverbWidth
        {
            get { return sfReverbWidth; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 100f);
                if (sfReverbWidth != newval)
                {
                    sfReverbWidth = newval;
                    SetParamSfReverb();
                }
            }
        }

        [HideInInspector, SerializeField]
        private float sfReverbWidth = 0.5f;

        /// <summary>
        /// [MPTK PRO] - Set the SoundFont reverb effect level\n
        /// V2.88.2 
        /// </summary>
        [HideInInspector]
        public float MPTK_SFReverbLevel
        {
            get { return sfReverbLevel; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 1f);
                if (sfReverbLevel != newval)
                {
                    sfReverbLevel = newval;
                    SetParamSfReverb();
                }
            }
        }

        [HideInInspector, SerializeField]
        private float sfReverbLevel = 0.9f;

        /// <summary>
        /// [MPTK PRO] - Set Reverb SoundFont default value as defined in fluidsynth.\n
        /// FLUID_REVERB_DEFAULT_ROOMSIZE 0.2f \n
        /// FLUID_REVERB_DEFAULT_DAMP 0.0f     \n
        /// FLUID_REVERB_DEFAULT_WIDTH 0.5f    \n
        /// FLUID_REVERB_DEFAULT_LEVEL 0.9f    \n
        /// </summary>
        [HideInInspector]
        public void MPTK_SFReverbSetDefault()
        {
            MPTK_SFReverbAmplify = 0f;
            MPTK_SFReverbRoomSize = 0.2f;
            MPTK_SFReverbDamp = 0f;
            MPTK_SFReverbWidth = 0.5f;
            MPTK_SFReverbLevel = 0.9f;
        }

        /**< Default chorus voice count */
        const int FLUID_CHORUS_DEFAULT_N = 3;

        /// <summary>
        /// [MPTK PRO] - Set the SoundFont chorus effect level [0, 10]\n
        /// V2.88.2 - becomes a parameter and default value set to 0.9 (was 2f, thank John)
        /// </summary>
        [HideInInspector]
        public float MPTK_SFChorusLevel
        {
            get { return sfChorusLevel; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 10f);
                if (sfChorusLevel != newval)
                {
                    sfChorusLevel = newval;
                    SetParamSfChorus();
                }
            }
        }
        [HideInInspector, SerializeField]
        private float sfChorusLevel = 0.9f; // was 2.0 in fluidsynth  ... but too much

        /// <summary>
        /// [MPTK PRO] - Set the SoundFont chorus effect speed\n
        /// Chorus speed in Hz [0.1, 5]\n
        /// V2.88.2
        /// </summary>
        [HideInInspector]
        public float MPTK_SFChorusSpeed
        {
            get { return sfChorusSpeed; }
            set
            {
                float newval = Mathf.Clamp(value, 0.1f, 5f);
                if (sfChorusSpeed != newval)
                {
                    sfChorusSpeed = newval;
                    SetParamSfChorus();
                }
            }
        }
        [HideInInspector, SerializeField]
        private float sfChorusSpeed = 0.3f;


        /// <summary>
        /// [MPTK PRO] - Set the SoundFont chorus effect depth\n
        /// Chorus depth [0, 256]\n
        /// V2.88.2
        /// </summary>
        [HideInInspector]
        public float MPTK_SFChorusDepth
        {
            get { return sfChorusDepth; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 256f);
                if (sfChorusDepth != newval)
                {
                    sfChorusDepth = newval;
                    SetParamSfChorus();
                }
            }
        }
        [HideInInspector, SerializeField]
        private float sfChorusDepth = 8f;

        /// <summary>
        /// [MPTK PRO] - Set the SoundFont chorus effect width\n
        /// The chorus unit process a monophonic input signal and produces stereo output controlled by WIDTH macro.\n
        /// Width allows to get a gradually stereo effect from minimum (monophonic) to maximum stereo effect. [0, 10]\n
        /// V2.88.2
        /// </summary>
        [HideInInspector]
        public float MPTK_SFChorusWidth
        {
            get { return sfChorusWidth; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 10f);
                if (sfChorusWidth != newval)
                {
                    sfChorusWidth = newval;
                    SetParamSfChorus();
                }
            }
        }
        [HideInInspector, SerializeField]
        private float sfChorusWidth = 10f;

        const fluid_chorus.fluid_chorus_mod FLUID_CHORUS_DEFAULT_TYPE = fluid_chorus.fluid_chorus_mod.FLUID_CHORUS_MOD_SINE;  /**< Default chorus waveform type */

        /// <summary>
        /// [MPTK PRO] - Set Chrous SoundFont default value as defined in fluidsynth.\n
        /// FLUID_CHORUS_DEFAULT_N 3        \n
        /// FLUID_CHORUS_DEFAULT_LEVEL 2.0 but set to 0.9 (thank John) \n
        /// FLUID_CHORUS_DEFAULT_SPEED 0.3 \n
        /// FLUID_CHORUS_DEFAULT_DEPTH 8.0 \n
        /// FLUID_CHORUS_DEFAULT_TYPE FLUID_CHORUS_MOD_SINE \n
        /// WIDTH 10
        /// </summary>
        public void MPTK_SFChorusSetDefault()
        {
            MPTK_SFChorusAmplify = 0f;
            MPTK_SFChorusLevel = 0.9f; // 2.0 in fluidsynthn set to 0.9 
            MPTK_SFChorusSpeed = 0.3f;
            MPTK_SFChorusDepth = 8f;
            MPTK_SFChorusWidth = 10f;
        }


        // ------------------------
        // Apply effect from Unity
        // ------------------------

        // -------
        // Reverb
        // -------

        /// <summary>
        /// [MPTK PRO] - Set Reverb Unity default value as defined with Unity.
        /// </summary>
        public void MPTK_ReverbSetDefault()
        {
            MPTK_ReverbDryLevel = Mathf.InverseLerp(-10000f, 0f, 0f);
            MPTK_ReverbRoom = Mathf.InverseLerp(-10000f, 0f, -1000f);
            MPTK_ReverbRoomHF = Mathf.InverseLerp(-10000f, 0f, -100f);
            MPTK_ReverbRoomLF = Mathf.InverseLerp(-10000f, 0f, 0f);
            MPTK_ReverbDecayTime = 1.49f;
            MPTK_ReverbDecayHFRatio = 0.83f;
            MPTK_ReverbReflectionLevel = Mathf.InverseLerp(-10000f, 1000f, -2602f);
            MPTK_ReverbReflectionDelay = Mathf.InverseLerp(-10000f, 1000f, -10000f);
            MPTK_ReverbLevel = Mathf.InverseLerp(-10000f, 2000f, 200f);
            MPTK_ReverbDelay = 0.011f;
            MPTK_ReverbHFReference = 5000f;
            MPTK_ReverbLFReference = 250f;
            MPTK_ReverbDiffusion = Mathf.InverseLerp(0f, 100f, 100f);
            MPTK_ReverbDensity = Mathf.InverseLerp(0f, 100f, 100f);
        }

        /// <summary>
        /// [MPTK PRO] - Apply Reverb Unity effect to the AudioSource. The effect is applied to all voices.
        /// </summary>
        [HideInInspector]
        public bool MPTK_ApplyUnityReverb
        {
            get { return applyReverb; }
            set { if (ReverbFilter != null) ReverbFilter.enabled = value; applyReverb = value; }
        }
        [HideInInspector, SerializeField]
        private bool applyReverb;

        [HideInInspector, SerializeField]
        private float reverbRoom, reverbRoomHF, reverbRoomLF, reverbReflectionLevel, reverbReflectionDelay, reverbDryLevel;

        [HideInInspector, SerializeField]
        private float reverbDecayTime, reverbDecayHFRatio, reverbLevel, reverbDelay, reverbHfReference, reverbLfReference, reverbDiffusion, reverbDensity;

        /// <summary>
        /// [MPTK PRO] - Mix level of dry signal in output.\n
        /// Ranges from 0 to 1. 
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbDryLevel
        {
            get { return reverbDryLevel; }
            set { reverbDryLevel = value; if (ReverbFilter != null) ReverbFilter.dryLevel = Mathf.Lerp(-10000f, 0f, reverbDryLevel); }
        }

        /// <summary>
        /// [MPTK PRO] - Room effect level at low frequencies.\n
        /// Ranges from 0 to 1.
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbRoom
        {
            get { return reverbRoom; }
            set { reverbRoom = value; if (ReverbFilter != null) ReverbFilter.room = Mathf.Lerp(-10000f, 0f, reverbRoom); }
        }

        /// <summary>
        /// [MPTK PRO] - Room effect high-frequency level.\n
        /// Ranges from 0 to 1.
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbRoomHF
        {
            get { return reverbRoomHF; }
            set { reverbRoomHF = value; if (ReverbFilter != null) ReverbFilter.roomHF = Mathf.Lerp(-10000f, 0f, reverbRoomHF); }
        }

        /// <summary>
        /// [MPTK PRO] - Room effect low-frequency level.\n
        /// Ranges from 0 to 1.
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbRoomLF
        {
            get { return reverbRoomLF; }
            set { reverbRoomLF = value; if (ReverbFilter != null) ReverbFilter.roomLF = Mathf.Lerp(-10000f, 0f, reverbRoomLF); }
        }

        /// <summary>
        /// [MPTK PRO] - Reverberation decay time at low-frequencies in seconds.\n
        /// Ranges from 0.1 to 20. Default is 1.
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbDecayTime
        {
            get { return reverbDecayTime; }
            set { reverbDecayTime = value; if (ReverbFilter != null) ReverbFilter.decayTime = reverbDecayTime; }
        }


        /// <summary>
        /// [MPTK PRO] - Decay HF Ratio : High-frequency to low-frequency decay time ratio.\n
        /// Ranges from 0.1 to 2.0.
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbDecayHFRatio
        {
            get { return reverbDecayHFRatio; }
            set { reverbDecayHFRatio = value; if (ReverbFilter != null) ReverbFilter.decayHFRatio = reverbDecayHFRatio; }
        }

        /// <summary>
        /// [MPTK PRO] - Early reflections level relative to room effect.\n
        /// Ranges from 0 to 1.
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbReflectionLevel
        {
            get { return reverbReflectionLevel; }
            set { reverbReflectionLevel = value; if (ReverbFilter != null) ReverbFilter.reflectionsLevel = Mathf.Lerp(-10000f, 1000f, reverbReflectionLevel); }
        }

        /// <summary>
        /// [MPTK PRO] - Late reverberation level relative to room effect.\n
        /// Ranges from -10000.0 to 2000.0. Default is 0.0.
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbReflectionDelay
        {
            get { return reverbReflectionDelay; }
            set { reverbReflectionDelay = value; if (ReverbFilter != null) ReverbFilter.reflectionsDelay = Mathf.Lerp(-10000f, 1000f, reverbReflectionDelay); }
        }

        /// <summary>
        /// [MPTK PRO] - Late reverberation level relative to room effect.\n
        /// Ranges from 0 to 1. 
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbLevel
        {
            get { return reverbLevel; }
            set { reverbLevel = value; if (ReverbFilter != null) ReverbFilter.reverbLevel = Mathf.Lerp(-10000f, 2000f, reverbLevel); }
        }

        /// <summary>
        /// [MPTK PRO] - Late reverberation delay time relative to first reflection in seconds.\n
        /// Ranges from 0 to 0.1. Default is 0.04
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbDelay
        {
            get { return reverbDelay; }
            set { reverbDelay = value; if (ReverbFilter != null) ReverbFilter.reverbDelay = reverbDelay; }
        }

        /// <summary>
        /// [MPTK PRO] - Reference high frequency in Hz.\n
        /// Ranges from 1000 to 20000. Default is 5000
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbHFReference
        {
            get { return reverbHfReference; }
            set { reverbHfReference = value; if (ReverbFilter != null) ReverbFilter.hfReference = reverbHfReference; }
        }

        /// <summary>
        /// [MPTK PRO] - Reference low-frequency in Hz.\n
        /// Ranges from 20 to 1000. Default is 250
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbLFReference
        {
            get { return reverbLfReference; }
            set { reverbLfReference = value; if (ReverbFilter != null) ReverbFilter.lfReference = reverbLfReference; }
        }

        /// <summary>
        /// [MPTK PRO] - Reverberation diffusion (echo density) in percent.\n
        /// Ranges from 0 to 1. Default is 1.
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbDiffusion
        {
            get { return reverbDiffusion; }
            set { reverbDiffusion = value; if (ReverbFilter != null) ReverbFilter.diffusion = Mathf.Lerp(0f, 100f, reverbDiffusion); }
        }

        /// <summary>
        /// [MPTK PRO] - Reverberation density (modal density) in percent.\n
        /// Ranges from 0 to 1.
        /// </summary>
        [HideInInspector]
        public float MPTK_ReverbDensity
        {
            get { return reverbDensity; }
            set { reverbDensity = value; if (ReverbFilter != null) ReverbFilter.density = Mathf.Lerp(0f, 100f, reverbDensity); }
        }

        // -------
        // Chorus
        // -------

        /// <summary>
        /// [MPTK PRO] - Set Chorus Unity default value as defined with Unity.
        /// </summary>
        public void MPTK_ChorusSetDefault()
        {
            MPTK_ChorusDryMix = 0.5f;
            MPTK_ChorusWetMix1 = 0.5f;
            MPTK_ChorusWetMix2 = 0.5f;
            MPTK_ChorusWetMix3 = 0.5f;
            MPTK_ChorusDelay = 40f;
            MPTK_ChorusRate = 0.8f;
            MPTK_ChorusDepth = 0.03f;
        }

        [HideInInspector, SerializeField]
        private bool applyChorus;

        [HideInInspector, SerializeField]
        private float chorusDryMix, chorusWetMix1, chorusWetMix2, chorusWetMix3, chorusDelay, chorusRate, chorusDepth;
        /// <summary>
        /// [MPTK PRO] - Apply Chorus Unity effect to the AudioSource. The effect is applied to all voices.
        /// </summary>
        [HideInInspector]
        public bool MPTK_ApplyUnityChorus
        {
            get { return applyChorus; }
            set { if (ChorusFilter != null) ChorusFilter.enabled = value; applyChorus = value; }
        }

        /// <summary>
        /// [MPTK PRO] - Volume of original signal to pass to output.\n
        /// Range from 0 to 1. Default = 0.5.
        /// </summary>
        [HideInInspector]
        public float MPTK_ChorusDryMix
        {
            get { return chorusDryMix; }
            set { chorusDryMix = value; if (ChorusFilter != null) ChorusFilter.dryMix = chorusDryMix; }
        }

        /// <summary>
        /// [MPTK PRO] - Volume of 1st chorus tap.\n
        /// Range from  0 to 1. Default = 0.5.
        /// </summary>
        [HideInInspector]
        public float MPTK_ChorusWetMix1
        {
            get { return chorusWetMix1; }
            set { chorusWetMix1 = value; if (ChorusFilter != null) ChorusFilter.wetMix1 = chorusWetMix1; }
        }

        /// <summary>
        /// [MPTK PRO] - Volume of 2nd chorus tap. This tap is 90 degrees out of phase of the first tap.\n
        /// Range from  0 to 1. Default = 0.5.
        /// </summary>
        [HideInInspector]
        public float MPTK_ChorusWetMix2
        {
            get { return chorusWetMix2; }
            set { chorusWetMix2 = value; if (ChorusFilter != null) ChorusFilter.wetMix2 = chorusWetMix2; }
        }

        /// <summary>
        /// [MPTK PRO] - Volume of 3rd chorus tap. This tap is 90 degrees out of phase of the second tap.\n
        /// Range from 0 to 1. Default = 0.5.
        /// </summary>
        [HideInInspector]
        public float MPTK_ChorusWetMix3
        {
            get { return chorusWetMix3; }
            set { chorusWetMix3 = value; if (ChorusFilter != null) ChorusFilter.wetMix3 = chorusWetMix3; }
        }

        /// <summary>
        /// [MPTK PRO] - Chorus delay in ms.\n
        /// Range from 0.1 to 100. Default = 40 ms.
        /// </summary>
        [HideInInspector]
        public float MPTK_ChorusDelay
        {
            get { return chorusDelay; }
            set { chorusDelay = value; if (ChorusFilter != null) ChorusFilter.delay = chorusDelay; }
        }

        /// <summary>
        /// [MPTK PRO] - Chorus modulation rate in hz.\n
        /// Range from 0 to 20. Default = 0.8 hz.
        /// </summary>
        [HideInInspector]
        public float MPTK_ChorusRate
        {
            get { return chorusRate; }
            set { chorusRate = value; if (ChorusFilter != null) ChorusFilter.rate = chorusRate; }
        }

        /// <summary>
        /// [MPTK PRO] - Chorus modulation depth.\n
        /// Range from 0 to 1. Default = 0.03.
        /// </summary>
        [HideInInspector]
        public float MPTK_ChorusDepth
        {
            get { return chorusDepth; }
            set { chorusDepth = value; if (ChorusFilter != null) ChorusFilter.depth = chorusDepth; }
        }

        private void InitEffect()
        {
            GenModifier.InitListGenerator();

            if (CoreAudioSource != null)
            {
                ReverbFilter = CoreAudioSource.GetComponent<AudioReverbFilter>();
                ReverbFilter.enabled = MPTK_ApplyUnityReverb;

                ChorusFilter = CoreAudioSource.GetComponent<AudioChorusFilter>();
                ChorusFilter.enabled = MPTK_ApplyUnityChorus;

                ///* Effects audio buffers */
                /* allocate the reverb module */
                fx_reverb = new float[FLUID_BUFSIZE];
                reverb = new fluid_revmodel(OutputRate, FLUID_BUFSIZE);
                SetParamSfReverb();

                fx_chorus = new float[FLUID_BUFSIZE];
                /* allocate the chorus module */
                chorus = new fluid_chorus(OutputRate, FLUID_BUFSIZE);
                SetParamSfChorus();
            }
        }

        private void SetParamSfReverb()
        {
            if (reverb != null)
                reverb.fluid_revmodel_set(/*(int)fluid_revmodel.fluid_revmodel_set_t.FLUID_REVMODEL_SET_ALL*/0xFF,
                    MPTK_SFReverbRoomSize, MPTK_SFReverbDamp, MPTK_SFReverbWidth, MPTK_SFReverbLevel);
        }

        public void SetParamSfChorus()
        {
            if (chorus != null)
                chorus.fluid_chorus_set((int)fluid_chorus.fluid_chorus_set_t.FLUID_CHORUS_SET_ALL,
                    FLUID_CHORUS_DEFAULT_N, MPTK_SFChorusLevel, MPTK_SFChorusSpeed, MPTK_SFChorusDepth, FLUID_CHORUS_DEFAULT_TYPE, MPTK_SFChorusWidth);
        }
        private void PrepareBufferEffect(out float[] reverb_buf, out float[] chorus_buf)
        {
            // Set up the reverb / chorus buffers only, when the effect is enabled on synth level.
            // Nonexisting buffers are detected in theDSP loop. 
            // Not sending the reverb / chorus signal saves some time in that case.
            if (MPTK_ApplySFReverb)
            {
                Array.Clear(fx_reverb, 0, FLUID_BUFSIZE);
                reverb_buf = fx_reverb;
            }
            else
                reverb_buf = null;

            if (MPTK_ApplySFChorus)
            {
                Array.Clear(fx_chorus, 0, FLUID_BUFSIZE);
                chorus_buf = fx_chorus;
            }
            else
                chorus_buf = null;
        }

        private void ProcessEffect(float[] reverb_buf, float[] chorus_buf)
        {
            /* send to reverb */
            if (MPTK_ApplySFReverb && reverb_buf != null)
            {
                reverb.fluid_revmodel_processmix(reverb_buf, left_buf, right_buf);
            }

            /* send to chorus */
            if (MPTK_ApplySFChorus && chorus_buf != null)
            {
                chorus.fluid_chorus_processmix(chorus_buf, left_buf, right_buf);
            }
        }

        public enum ModeSpatializer
        {
            Channel, Track,
        }

        /// <summary>
        /// Trus if this MidiSynth is the master synth responsible to read midi events and to dispatch to other MidiSynths
        /// </summary>
        public bool MPTK_IsSpatialSynthMaster { get { return isSpatialSynthMaster; } }
        protected bool isSpatialSynthMaster = true; // for internal use, true only for the master midisynth responsible to read events, false for slave midisynth responsible to play note

        [HideInInspector]
        public ModeSpatializer MPTK_ModeSpatializer;

        [HideInInspector]
        public int MPTK_MaxSpatialSynth;


        /// <summary>
        /// In spatialization mode not all MidiSynths are enabled.
        /// </summary>
        [HideInInspector]
        public bool MPTK_SpatialSynthEnabled;

        /// <summary>
        /// If spatialization is track mode, contains the last instrument played on this track
        /// </summary>
        public string MPTK_InstrumentPlayed { get { return string.IsNullOrEmpty(instrumentPlayed) ? "" : instrumentPlayed; } }
        private string instrumentPlayed;

        // Play each midi events from the Midi reader (master synth) by sending midi events to the dedicated synth
        private void PlaySpatialEvent(MPTKEvent midievent)
        {
            if (MPTK_ModeSpatializer == ModeSpatializer.Channel)
                SpatialSynths[midievent.Channel].MPTK_PlayDirectEvent((MPTKEvent)midievent/*.Clone()*/, false);
            else
            {
                if (midievent.Track < MPTK_MaxSpatialSynth)
                {
                    MidiSpatializer spatializer = (MidiSpatializer)SpatialSynths[(int)midievent.Track];
                    if (spatializer.MPTK_SpatialSynthEnabled)
                    {
                        switch (midievent.Command)
                        {
                            case MPTKCommand.NoteOn:
                                // Find which instrument will be played on this track
                                spatializer.instrumentPlayed = spatializer.MPTK_ChannelPresetGetName(midievent.Channel);
                                //Debug.Log($"{midievent.Track} {midievent.Channel} {spatializer.instrumentPlayed}");
                                spatializer.MPTK_PlayDirectEvent((MPTKEvent)midievent/*.Clone()*/, false);
                                break;
                            //case MPTKCommand.NoteOff: --- send noteoff to all track because some midi send note off on a different track than note on !
                            //    spatializer.MPTK_PlayDirectEvent((MPTKEvent)midievent/*.Clone()*/, false);
                            //    break;

                            default:
                                foreach (MidiFilePlayer mfp in SpatialSynths)
                                    mfp.MPTK_PlayDirectEvent((MPTKEvent)midievent/*.Clone()*/, false);
                                break;
                        }
                    }
                }
                else
                    Debug.LogWarning($"Not enough Spatial Synths available Track:{midievent.Track} Max:{MPTK_MaxSpatialSynth}");
            }
        }

        // Send midi events to the UI thru the OnEventNotesMidi event
        protected void SpatialSendEvents(List<MPTKEvent> midievents)
        {
            if (midievents.Count == 1)
            {
                int indexSynth = (int)(MPTK_ModeSpatializer == ModeSpatializer.Channel ? midievents[0].Channel : midievents[0].Track);
                SpatialSynths[indexSynth].OnEventNotesMidi.Invoke(midievents);
            }
            else
            {
                // Send to the channel synth
                List<MPTKEvent> synthEvents = new List<MPTKEvent>();
                foreach (MPTKEvent midievent in midievents)
                {
                    int indexSynth = (int)(MPTK_ModeSpatializer == ModeSpatializer.Channel ? midievent.Channel : midievent.Track);
                    if (SpatialSynths[indexSynth].OnEventNotesMidi != null)
                    {
                        synthEvents.Clear();
                        synthEvents.Add(midievent);
                        SpatialSynths[indexSynth].OnEventNotesMidi.Invoke(synthEvents);
                    }
                }
            }
        }


        private void BuildSpatialSynth()
        {
            // Only the main midi reader instanciate all the others synths
            if (this is MidiSpatializer && MPTK_SpatialSynthIndex < 0)
            {
                MPTK_MaxSpatialSynth = Mathf.Clamp(MPTK_MaxSpatialSynth, 16, 100);
                SpatialSynths = new List<MidiFilePlayer>();//  new MidiFilePlayer[16];
                for (int idSynth = 0; idSynth < MPTK_MaxSpatialSynth; idSynth++)
                {
                    // Bad parameters could exec infinite loop, bodyguard below
                    if (lastIdSynth > 100) break;
                    //Debug.Log($"Before Instantiate synth  IdSynth:{IdSynth} channel:{channel}");
                    MidiFilePlayer mfp = Instantiate<MidiFilePlayer>((MidiFilePlayer)this);
                    //Debug.Log($"After Instantiate synth mfp.IdSynth:{mfp.IdSynth} mfp.IsMidiReader:{mfp.IsMidiReader}");
                    //mfp.IsMidiReader = false;
                    mfp.spatialSynthIndex = idSynth;
                    //mfp.name = $"Synth {IdSynth} {mfp.IdSynth} Channel:{channel}";
                    mfp.name = $"Synth Id{idSynth + 1}";
                    mfp.MPTK_PlayOnStart = false;
                    mfp.MPTK_InitSynth();
                    mfp.MPTK_Spatialize = true;
                    mfp.isSpatialSynthMaster = false;
                    SpatialSynths.Add(mfp);
                }
                // Avoid set parent in the previous loop because infinite loop are created. Why? I don't known!!!
                foreach (MidiFilePlayer mfp in SpatialSynths) mfp.transform.SetParent(this.transform);
            }
        }

        private void StartFrame()
        {
            if (OnAudioFrameStart != null)
                OnAudioFrameStart.Invoke(SynthElapsedMilli);
        }

        private void StartMidiEvent(MPTKEvent midi)
        {
            if (OnMidiEvent != null)
                OnMidiEvent(midi);
        }
    }

#if UNITY_ANDROID && UNITY_OBOE
        private void InitOboe()
        {
            OboeManager.Initialize();
            Mixer mixer = new Mixer();
            AudioStream audioStream;

            if (VerboseSynth)
                Debug.Log($"Init Oboe {MPTK_SynthRate} {DspBufferSize}");

            using (AudioStreamBuilder audioStreamBuilder = new AudioStreamBuilder
            {
                Format = AudioFormat.Float,
                ChannelCount = 2,
                SampleRate = MPTK_SynthRate, // 48000,
                Callback = mixer,
                AudioApi = AudioApi.Unspecified,
                PerformanceMode = PerformanceMode.LowLatency,
                SharingMode = SharingMode.Exclusive,
                BufferCapacityInFrames = DspBufferSize, // 384,
                IsFormatConversionAllowed = true,
                Direction = Direction.Output
            })
            {
                Result result = audioStreamBuilder.OpenStream(out audioStream);
                if (result != Result.OK)
                    Debug.Log($"Oboe Error - result:{result} ");
            }
            mixer.clipPlayers.Add(this);
            audioStream.Start();
        }
        //Debug.Log($"OnAudioData {audioStream.BytesPerFrame} FramesPerCallback:{audioStream.FramesPerCallback} BufferCapacityInFrames:{audioStream.BufferCapacityInFrames} BufferSizeInFrames:{audioStream.BufferSizeInFrames} numFrames:{numFrames} DspBufferSize:{DspBufferSize}");
        // Avec BufferCapacityInFrames = 384,
        // BytesPerFrame:8 BytesPerSample:4 ChannelCount:2 BufferSizeInFrames:384 (3*2*64),
        // FramesPerCallback=0 BufferCapacityInFrames:384 BufferSizeInFrames:384
        //numFrames:192,128,64

        // Avec BufferCapacityInFrames = 2048,
        // BytesPerFrame:8 BufferSizeInFrames:384 (3*2*64 ?)
        // FramesPerCallback=0 BufferCapacityInFrames:2048 BufferSizeInFrames:384
        //numFrames:192,128,64

#endif
}

