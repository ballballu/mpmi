
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System;
using UnityEngine.Events;
using System.Net;
using MEC;

namespace MidiPlayerTK
{
    /// <summary>
    /// [MPTK PRO] - Script associated to the prefab MidiExternalPlayer.\n
    /// Play a midi file from a path on the local deskop or from a web site.\n 
    /// There is no need to writing a script. For a simple usage, all the job can be done in the prefab inspector.\n\n
    /// But a set of API is also available to drive the music from your script.\n
    /// On top of that, this class inherits from MidiFilePlayer and MidiSynth\n
    /// All properties, event, methods from MidiFilePlayer and MidiSynth are available in this class.\n\n
    /// @code
    /// // Example of script. See TestMidiExternalPlayer.cs for a more detailed usage.
    /// // Need for a reference to the Prefab (to be set in the hierarchy or can be done by script)
    /// MidiExternalPlayer midiExternalPlayer;
    /// 
    /// if (midiExternalPlayer==null)  
    ///    Debug.LogError("TestMidiExternalPlayer: there is no MidiExternalPlayer Prefab set in Inspector.");
    ///    
    /// midiExternalPlayer.MPTK_MidiName = "http://www.midiworld.com/midis/other/c2/bolero.mid";
    /// midiExternalPlayer.MPTK_Play();
    /// @endcode
    /// </summary>
    [HelpURL("https://paxstellar.fr/midi-external-player-v2/")]
    public class MidiExternalPlayer : MidiFilePlayer
    {
        /// <summary>
        /// Full path to Midi file or URL to play. Must start with file:// or http:// or https://.
        /// Example: MPTK_MidiName="http://www.midiworld.com/midis/other/c2/bolero.mid";
        /// See https://en.wikipedia.org/wiki/File_URI_scheme for example of URI file
        /// </summary>
        public new string MPTK_MidiName
        {
            get
            {
                return pathmidiNameToPlay;
            }
            set
            {
                pathmidiNameToPlay = value.Trim();
                base.MPTK_MidiName = pathmidiNameToPlay;
            }
        }
        [SerializeField]
        [HideInInspector]
        private string pathmidiNameToPlay;

        protected new void Awake()
        {
            //Debug.Log("Awake MidiExternalPlayer:" + MPTK_IsPlaying + " " + MPTK_PlayOnStart + " " + MPTK_IsPaused);
            base.AwakeMidiFilePlayer(); // V2.83
            //base.Awake(); 
        }

        protected new void Start()
        {
            //Debug.Log("Start MidiExternalPlayer:" + MPTK_IsPlaying + " " + MPTK_PlayOnStart + " " + MPTK_IsPaused);
            base.StartMidiFilePlayer(); // V2.83

            OnEventStartPlayMidi.AddListener((string midiname) =>
            {
                //Debug.Log($"Start playing {midiname}");
                MPTK_StatusLastMidiLoaded = 0;
            });

            OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) =>
            {
                //Debug.Log($"End playing {midiname} {reason}");
                //if (reason == EventEndMidiEnum.MidiErr && MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.NotYetDefined)
                //    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.MidiFileInvalid;
            });
        }

        /// <summary>
        /// Play the midi file defined in MPTK_MidiName
        /// </summary>
        public override void MPTK_Play()
        {
            try
            {
                MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NotYetDefined;

                if (MidiPlayerGlobal.MPTK_SoundFontLoaded)
                {
                    playPause = false;

                    if (!MPTK_IsPlaying)
                    {
                        if (string.IsNullOrEmpty(pathmidiNameToPlay))
                        {
                            //Debug.LogWarning("MPTK_Play: set MPTK_MidiName or Midi Url/path in inspector before playing");
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.MidiNameNotDefined;
                        }
                        else if (!pathmidiNameToPlay.ToLower().StartsWith("file://") &&
                                !pathmidiNameToPlay.ToLower().StartsWith("http://") &&
                                !pathmidiNameToPlay.ToLower().StartsWith("https://"))
                        {
                            //Debug.LogWarning("MPTK_MidiName must start with file:// or http:// or https:// - found: '" + pathmidiNameToPlay + "'");
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.MidiNameInvalid;
                        }
                        else if (pathmidiNameToPlay.ToLower().StartsWith("file://") && !File.Exists(pathmidiNameToPlay.Remove(0, 7)))
                        {
                            //Debug.LogWarning("Midi file not found '" + pathmidiNameToPlay + "'");
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NotFound;
                        }
                    }
                    else
                    {
                        //Debug.LogWarning("Already playing - " + pathmidiNameToPlay);
                        MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.AlreadyPlaying;
                    }
                }
                else
                {
                    //Debug.LogWarning("SoundFont not loaded");
                    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.SoundFontNotLoaded;
                }

                // If no error, load and play the midi in background
                if (MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.NotYetDefined)
                {
                    MPTK_InitSynth();
                    MPTK_StartSequencerMidi();
                    Routine.RunCoroutine(TheadLoadDataAndPlay(), Segment.RealtimeUpdate);
                }
                else
                {
                    OnEventEndPlayMidi.Invoke(pathmidiNameToPlay, EventEndMidiEnum.MidiErr);
                }
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Play the midi file from a byte array (MPTK_MidiName can be used is not used for loading data, but only for information in call back)
        /// </summary>
        public void MPTK_Play(byte[] data)
        {
            try
            {
                MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NotYetDefined;

                if (MidiPlayerGlobal.MPTK_SoundFontLoaded)
                {
                    playPause = false;

                    if (!MPTK_IsPlaying)
                    {
                        if (data == null)
                        {
                            //Debug.LogWarning("MPTK_Play: set MPTK_MidiName or Midi Url/path in inspector before playing");
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.MidiNameNotDefined;
                        }
                        else if (data.Length < 4)
                        {
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.TooShortSize;
                            //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Not a midi file, too short size");
                        }
                        else if (System.Text.Encoding.Default.GetString(data, 0, 4) != "MThd")
                        {
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NoMThdSignature;
                            //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Not a midi file, signature MThd not found");
                        }
                    }
                    else
                    {
                        //Debug.LogWarning("Already playing - " + pathmidiNameToPlay);
                        MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.AlreadyPlaying;
                    }
                }
                else
                {
                    //Debug.LogWarning("SoundFont not loaded");
                    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.SoundFontNotLoaded;
                }

                // If no error, load and play the midi in background
                if (MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.NotYetDefined)
                {
                    MPTK_InitSynth();
                    MPTK_StartSequencerMidi();

                    // Start playing
                    if (MPTK_CorePlayer)
                        Routine.RunCoroutine(ThreadCorePlay(data).CancelWith(gameObject), Segment.RealtimeUpdate);
                    else
                        Routine.RunCoroutine(ThreadPlay(data).CancelWith(gameObject), Segment.RealtimeUpdate);
                }
                else
                {
                    OnEventEndPlayMidi.Invoke(pathmidiNameToPlay, EventEndMidiEnum.MidiErr);
                }
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        ///// <summary>
        ///// Play the midi file from a byte array (MPTK_MidiName can be used is not used for loading data, but only for information in call back)
        ///// </summary>
        //public void MPTK_Play(MidiFileWriter2 mfw2)
        //{
        //    try
        //    {
        //        MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NotYetDefined;

        //        if (MidiPlayerGlobal.MPTK_SoundFontLoaded)
        //        {
        //            playPause = false;

        //            if (!MPTK_IsPlaying)
        //            {
        //                if (mfw2 == null)
        //                {
        //                    //Debug.LogWarning("MPTK_Play: set MPTK_MidiName or Midi Url/path in inspector before playing");
        //                    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.MidiNameNotDefined;
        //                }
        //                //else if (mfw2.Length < 4)
        //                //{
        //                //    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.TooShortSize;
        //                //    //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Not a midi file, too short size");
        //                //}
        //            }
        //            else
        //            {
        //                //Debug.LogWarning("Already playing - " + pathmidiNameToPlay);
        //                MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.AlreadyPlaying;
        //            }
        //        }
        //        else
        //        {
        //            //Debug.LogWarning("SoundFont not loaded");
        //            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.SoundFontNotLoaded;
        //        }

        //        // If no error, load and play the midi in background
        //        if (MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.NotYetDefined)
        //        {
        //            MPTK_InitSynth();
        //            MPTK_StartSequencerMidi();
        //            MPTK_MidiName = string.IsNullOrEmpty(mfw2.MPTK_MidiName) ? "" : mfw2.MPTK_MidiName;
        //            // Start playing
        //            Routine.RunCoroutine(ThreadWritePlay(mfw2).CancelWith(gameObject), Segment.RealtimeUpdate);
        //        }
        //        else
        //        {
        //            OnEventEndPlayMidi.Invoke(pathmidiNameToPlay, EventEndMidiEnum.MidiErr);
        //        }
        //    }

        //    catch (System.Exception ex)
        //    {
        //        MidiPlayerGlobal.ErrorDetail(ex);
        //    }
        //}

        /// <summary>
        /// Load midi file in background and play
        /// </summary>
        /// <returns></returns>
        private IEnumerator<float> TheadLoadDataAndPlay()
        {
            base.MPTK_MidiName = pathmidiNameToPlay;
            using (UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(pathmidiNameToPlay))
            {
                yield return Routine.WaitUntilDone(req.SendWebRequest());
                byte[] data = null;
                //Debug.Log($"result:{req.result} {pathmidiNameToPlay}");
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.ConnectionError)
#else
                if (!req.isNetworkError)
#endif
                {
                    //Dictionary<string,string> headers=  req.GetResponseHeaders();
                    //foreach (string key in headers.Keys) Debug.Log($"'{key}'   '{headers[key]}'");

                    data = req.downloadHandler.data;
#if UNITY_2020_2_OR_NEWER
                    if (data == null || req.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError || req.result == UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError || req.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
#else
                    if (data == null || req.isHttpError)
#endif
                    {
                        MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NotFound;
                        //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Midi not found, response code: {req.responseCode}");
                    }
                    else if (data.Length < 4)
                    {
                        MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.TooShortSize;
                        //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Not a midi file, too short size");
                    }
                    else if (System.Text.Encoding.Default.GetString(data, 0, 4) != "MThd")
                    {
                        MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NoMThdSignature;
                        //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Not a midi file, signature MThd not found");
                    }
                }
                else
                {
                    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NetworkError; // Web site not found
                    //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Network error {req.error}");
                }

                if (MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.NotYetDefined)
                {
                    // Start playing
                    if (MPTK_CorePlayer)
                        Routine.RunCoroutine(ThreadCorePlay(data).CancelWith(gameObject), Segment.RealtimeUpdate);
                    else
                        Routine.RunCoroutine(ThreadPlay(data).CancelWith(gameObject), Segment.RealtimeUpdate);
                }
                else
                {
                    OnEventEndPlayMidi.Invoke(pathmidiNameToPlay, EventEndMidiEnum.MidiErr);
                }
            }
        }

      

        //        /// <summary>
        //        /// Drive the MidiPlayer system thread from the Unity thread
        //        /// </summary>
        //        /// <param name="midiBytesToPlay"></param>
        //        /// <param name="fromPosition">time to start in millisecond</param>
        //        /// <param name="toPosition">time to end in millisecond</param>
        //        /// <returns></returns>
        //        public IEnumerator<float> ThreadWritePlay(MidiFileWriter2 mfw2, float fromPosition = 0, float toPosition = 0)
        //        {
        //            StartPlaying();
        //            string currentMidiName = MPTK_MidiName;
        //            //Debug.Log("Start play " + fromPosition + " " + toPosition);
        //            try
        //            {

        //                midiLoaded = new MidiLoad();
        //                midiLoaded.KeepNoteOff = MPTK_KeepNoteOff;
        //                midiLoaded.EnableChangeTempo = MPTK_EnableChangeTempo;
        //                if (!midiLoaded.MPTK_Load(mfw2))
        //                    midiLoaded = null;
        //#if DEBUG_START_MIDI
        //                Debug.Log("After load midi " + (double)watchStartMidi.ElapsedTicks / ((double)System.Diagnostics.Stopwatch.Frequency / 1000d));
        //#endif
        //            }
        //            catch (System.Exception ex)
        //            {
        //                MidiPlayerGlobal.ErrorDetail(ex);
        //            }

        //            Routine.RunCoroutine(ThreadMidiPlaying(currentMidiName, fromPosition, toPosition).CancelWith(gameObject), Segment.RealtimeUpdate);
        //            yield return 0;
        //        }

        /// <summary>
        /// Not applicable for external
        /// </summary>
        public new MidiLoad MPTK_Load()
        {
            return null;
        }

        /// <summary>
        /// Not applicable for external
        /// </summary>
        public new int MPTK_MidiIndex
        {
            get
            {
                Debug.LogWarning("MPTK_MidiIndex not available for MidiExternalPlayer");
                return -1;
            }
            set
            {
                Debug.LogWarning("MPTK_MidiIndex not available for MidiExternalPlayer");
            }
        }

        /// <summary>
        /// Not applicable for external
        /// </summary>
        public new void MPTK_Next()
        {
            try
            {
                Debug.LogWarning("MPTK_Next not available for MidiExternalPlayer");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Not applicable for external
        /// </summary>
        public new void MPTK_Previous()
        {
            try
            {
                Debug.LogWarning("MPTK_Next not available for MidiExternalPlayer");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
    }
}

