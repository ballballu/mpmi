//#define DEBUGPERF
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System;
using UnityEngine.Events;
using MEC;
using System.Runtime.InteropServices;

namespace MidiPlayerTK
{
    /// <summary>
    /// [MPTK PRO] - Script associated to the prefab MidiInReader. \n
    /// Read Midi events from a Midi keyboard connected your device (Windows 10 or MacOS). See example of use in TestMidiInputScripting.cs\n
    /// There is no need to writing a script. For a simple usage, all the job can be done in the prefab inspector.\n
    /// @code
    /// // Example of script. See TestMidiInputScripting.cs for a more detailed usage.
    /// // Need for a reference to the Prefab (can also be set from the hierarchy)
    /// MidiInReader midiIn = FindObjectOfType<MidiInReader>();
    /// 
    /// if (midiIn == null) 
    ///     Debug.Log("Can't find a MidiInReader Prefab in the Hierarchy. No events will be read");
    ///     
    /// // There is two methods to trigger event: in inpector from the Unity editor or by script
    /// midiIn.OnEventInputMidi.AddListener((MPTKEvent evt) => 
    /// {
    ///     // your processing here
    ///     Debug.Log(evt.ToString());
    /// });
    /// @endcode
    /// </summary>
    [HelpURL("https://paxstellar.fr/prefab-midiinreader/")]
    [RequireComponent(typeof(AudioSource))]
    public class MidiInReader : MidiSynth
    {
        /// <summary>
        /// Read Midi input
        /// </summary>
        public bool MPTK_ReadMidiInput;

        public bool MPTK_RealTimeRead
        {
            get { return realTimeRead; }
            set
            {
                //Debug.Log($"MPTK_RealTimeRead {realTimeRead} --> {value}");
                realTimeRead = value;
                if (realTimeRead)
                {
                    MidiKeyboard.OnActionInputMidi += ProcessEvent;
                    MidiKeyboard.MPTK_SetRealTimeRead();
                }
                else
                {
                    MidiKeyboard.OnActionInputMidi -= ProcessEvent;
                    MidiKeyboard.MPTK_UnsetRealTimeRead();
                }
            }
        }

        [SerializeField]
        private bool realTimeRead;

        /// <summary>
        /// Log midi events
        /// </summary>
        public bool MPTK_LogEvents;

        public float MPTK_DelayToRefreshDeviceMilliSeconds = 500f;

        float timeTorefresh;

        public int MPTK_CountEndpoints
        {
            get
            {
                //Debug.Log("MPTK_CountEndpoints:" + CountEndpoints().ToString());
                return MidiKeyboard.MPTK_CountInp();
            }
        }

        public string MPTK_GetEndpointDescription(int index)
        {
            return string.Format("id:{0} name:{1}", index, MidiKeyboard.MPTK_GetInpName(index));
        }

        /// <summary>
        /// Define unity event to trigger when note available from the Midi file.
        /// @code
        /// MidiInReader midiFilePlayer = FindObjectOfType<MidiInReader>(); 
        ///         ...
        /// if (!midiFilePlayer.OnEventInputMidi.HasEvent())
        /// {
        ///    // No listener defined, set now by script. NotesToPlay will be called for each new notes read from Midi file
        ///    midiFilePlayer.OnEventInputMidi.AddListener(NotesToPlay);
        /// }
        ///         ...
        /// public void NotesToPlay(MPTKEvent notes)
        /// {
        ///    Debug.Log(notes.Value);
        ///    foreach (MPTKEvent midievent in notes)
        ///    {
        ///         ...
        ///    }
        /// }
        /// @endcode
        /// </summary>
        [HideInInspector]
        public EventMidiClass OnEventInputMidi;


        new void Awake()
        {
            base.Awake();
        }

        new void Start()
        {
            try
            {
                MidiKeyboard.MPTK_Init();

                MidiInReader[] list = FindObjectsOfType<MidiInReader>();
                if (list.Length > 1)
                {
                    Debug.LogWarning("No more than one MidiInReader must be present in your hierarchy, we found " + list.Length + " MidiInReader.");
                }
                MPTK_InitSynth();
                base.Start();
                // Always enabled for midi stream
                MPTK_EnablePresetDrum = true;
                ThreadDestroyAllVoice();
                timeTorefresh = 0f;

                if (MPTK_RealTimeRead)
                {
                    MidiKeyboard.MPTK_SetRealTimeRead();
                }
                // Force set or unset CB realtime
                //MPTK_RealTimeRead = realTimeRead;

            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        public void OnApplicationQuit()
        {
            //Debug.Log("OnApplicationQuit MPTK_UnsetRealTimeRead");
            MidiKeyboard.MPTK_UnsetRealTimeRead();
        }

        public static void ErrorMidiPlugin()
        {
            Debug.LogWarning("MidiPlugin not found, please see here a setup description https://paxstellar.fr/prefab-midiinreader");
        }

        void Update()
        {
            int count = 0;
            try
            {
                if (Time.fixedUnscaledTime > timeTorefresh)
                {
                    timeTorefresh = Time.fixedUnscaledTime + MPTK_DelayToRefreshDeviceMilliSeconds / 1000f;
                    //Debug.Log(Time.fixedUnscaledTime);
                    // Open or refresh midi input 
                    MidiKeyboard.MPTK_OpenAllInp();
                    MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
                    if (status != MidiKeyboard.PluginError.OK)
                        Debug.LogWarning($"Midi Keyboard error, status: {status}");
                }
                if (!MPTK_RealTimeRead)
                {
                    // Process the message queue and avoid locking Unity
                    while (MPTK_ReadMidiInput && count < 100)
                    {
                        count++;

                        MPTKEvent midievent = MidiKeyboard.MPTK_Read();

                        // No more Midi message
                        if (midievent == null)
                            break;

                        // Call event with these midi events
                        ProcessEvent(midievent);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private void ProcessEvent(MPTKEvent midievent)
        {
            try
            {
                if (OnEventInputMidi != null)
                    OnEventInputMidi.Invoke(midievent);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }

            if (MPTK_DirectSendToPlayer)
                MPTK_PlayDirectEvent(midievent);
                Debug.Log("---------------- here 19");

            if (MPTK_LogEvents)
                Debug.Log(midievent.ToString());
        }
    }
}

