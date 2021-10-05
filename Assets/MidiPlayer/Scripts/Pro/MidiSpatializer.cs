
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
    /// [MPTK PRO] - Script associated to the prefab MidiSpatializer.\n
    /// It's quite light because the major job is done with MidiSynth\n
    /// There is no specific API for this prefab.\n
    /// Scripting is necessary to defined position of channel or instrument in your 3D env. See below.\n\n
    /// On top of that, this class inherits from MidiFilePlayer and MidiSynth\n
    /// All properties, event, methods from MidiFilePlayer and MidiSynth are available in this class.\n\n
    /// ///! @snippet TestSpatializerFly.cs ExampleArrangeByChannel
    /// See full example in TestSpatializerFly.cs
    /// Available with V2.83.
    /// </summary>
    //  [HelpURL("https://paxstellar.fr/midi-external-player-v2/")]
    public class MidiSpatializer : MidiFilePlayer
    {
        protected new void Awake()
        {
            //Debug.Log("Awake MidiSpatializer:" + MPTK_IsPlaying + " " + MPTK_PlayOnStart + " " + MPTK_IsPaused);
            // Set this midisynth as the master : read midi events and send to the slave midisynth
            MPTK_Spatialize = true;
            if (!MPTK_CorePlayer)
            {
                Debug.LogWarning($"MidiSpatializer works only in Core player mode. Change properties in inspector");
                return;
            }

            if (MPTK_MaxDistance <= 0f)
                Debug.LogWarning($"Max Distance is set to 0, any sound will be played.");

            base.AwakeMidiFilePlayer();
        }

        public new void Start()
        {
            //Debug.Log("Start MidiSpatializer:" + MPTK_IsPlaying + " MPTK_DedicatedChannel:" + MPTK_DedicatedChannel + " MPTK_IsSpatialSynthMaster:" + MPTK_IsSpatialSynthMaster);
            if (!MPTK_CorePlayer)
                return;
            base.StartMidiFilePlayer();
        }

        public  void MPTK_DisableUnsedSynth(int countOfUsefulSynth)
        {
            foreach (MidiFilePlayer mfp in MidiFilePlayer.SpatialSynths)
            {
                if (MPTK_ModeSpatializer== ModeSpatializer.Channel)
                    if (mfp.MPTK_SpatialSynthIndex >= 16)
                        mfp.MPTK_SpatialSynthEnabled = false;
                    else
                        mfp.MPTK_SpatialSynthEnabled = true;
                else if (mfp.MPTK_SpatialSynthIndex >= countOfUsefulSynth)
                    mfp.MPTK_SpatialSynthEnabled = false;
                else
                    mfp.MPTK_SpatialSynthEnabled = true;
            }
        }
    }
}

