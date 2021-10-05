using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// [MPTK PRO] Chord builder class for MPTK. Usefull to generate Midi Music with MidiStreamPlayer - V2.82
    /// See example in TestMidiStream.cs and ExtStreamPlayerPro.cs
    /// </summary>
    public class MPTKChordBuilder
    {
        public enum Modifier3
        {
            Maj, // Accord majeur : tierce majeur, quinte juste ( 1 3 5 ). Do Mi Sol
            Min, // Accord mineur : tierce mineur, quinte juste ( 1 3b 5). Do Re# Sol
            Dim, // Accord diminué : tierce mineur, quinte diminuée ( 1 3b 5b). Do Re# Fa#
            DimHalf, // Accord demi-diminué : tierce majeur, quinte diminuée (1 3 5b). Do Mi Fa#
            Aug, // Accord augmenté : tierce majeur, quinte augmentée (1 3 5#).  Do Mi Sol#
            Sus2, // Accord suspendu 2 : la tierce est remplacée par une seconde (1 2 5). Do Re Sol
            Sus4, // Accord suspendu 4 : la tierce est remplacée par une quarte (1 4 5). Do Fa Sol 
        }

        public enum Modifier4
        {
            Maj6, // tierce majeure ou mineure et une sixieme majeure. Do Mi/Mib Sol La
            Min6, // tierce majeure ou mineure et une sixieme mineure. Do Mi/Mib Sol Lab
            Maj7, // tierce majeure ou mineure et une septième majeure. Do Mi/Mib Sol Si
            Min7, // tierce majeure ou mineure et une septième mineure. Do Mi/Mib Sol Sib
        }

        /// <summary>
        /// Tonic (Root) for the chord. 48=C4, ... , 60=C5, 61=C5#, 62=D5, ... , 72=C6, ....
        /// </summary>
        public int Tonic;

        /// <summary>
        /// Count of notes to compose the chord. Between 2 and 20.
        /// </summary>
        public int Count;

        /// <summary>
        /// Scale Degree. Between 1 and 7.
        ///! @li @c I   Tonic       First
        ///! @li @c II  Supertonic  Second
        ///! @li @c III Mediant     Maj or min Third
        ///! @li @c IV  Subdominant Fourth
        ///! @li @c V   Dominant    Fifth
        ///! @li @c VI  Submediant  Maj or min Sixth
        ///! @li @c VII Leading Tone/Subtonic Maj or min Seventh
        ///! Good reading here: https://lotusmusic.com/lm_chordnames.html
        /// </summary>
        public int Degree;

        /// <summary>
        /// Index of the chord in the libraries file ChordLib.csv in folder Resources/GeneratorTemplate.csv. To be used with MidiStreamPlayer.MPTK_PlayChordFromLib(MPTKChord chord)
        /// </summary>
        public int FromLib;

        /// <summary>
        /// Midi channel fom 0 to 15 (9 for drum)
        /// </summary>
        public int Channel;

        /// <summary>
        /// Velocity between 0 and 127
        /// </summary>
        public int Velocity;

        /// <summary>
        /// Duration of the chord in millisecond. Set -1 to play indefinitely.
        /// </summary>
        public long Duration;

        /// <summary>
        /// Delay in millisecond before playing the chord.
        /// </summary>
        public long Delay;

        /// <summary>
        /// Delay in millisecond between each notes in the chord (play an arpeggio).
        /// </summary>
        public long Arpeggio;

        /// <summary>
        /// List of midi events played for this chord. This list is build when call to MPTK_PlayChord or MPTK_PlayChordFromLib is done else null.
        /// </summary>
        public List<MPTKEvent> Events;

        //// https://www.bellandcomusic.com/building-chords.html
        //public bool Alterations;

        private bool logChord;

        /// <summary>
        /// Create a default chord: tonic=C4, degree=1, count note=3.
        /// </summary>
        /// <param name="log">True to display log</param>
        public MPTKChordBuilder(bool log = false)
        {
            logChord = log;
            Tonic = 48;
            Degree = 1;
            Count = 3;
            Duration = -1; // indefinitely
            Channel = 0;
            Delay = 0;
            Arpeggio = 0;
            Velocity = 127; // max
        }

        private long Clamp(long val, long min, long max)
        {
            return val > max ? max : val < min ? min : val;
        }

        /// <summary>
        /// [MPTK PRO] Build a chord from the current selected range (MPTK_RangeSelected), Tonic and Degree are to be defined in parameter MPTKChord chord.
        /// Major range is selected if no range defined. After the call, Events contains all notes for the chord.
        /// </summary>
        /// <param name="range"></param>
        public void MPTK_BuildFromRange(MPTKRangeLib range = null)
        {
            if (range == null) range = MPTKRangeLib.Range(0, logChord);
            Tonic = Mathf.Clamp(Tonic, 0, 127);
            Count = Mathf.Clamp(Count, 2, 20);
            Degree = Mathf.Clamp(Degree, 1, 7);
            Velocity = Mathf.Clamp(Velocity, 0, 127);
            Duration = Clamp(Duration, -1, 999999);
            Delay = Clamp(Delay, 0, 999999);
            Arpeggio = Clamp(Arpeggio, 0, 1000);

            Events = new List<MPTKEvent>();

            for (int iNote = 0; iNote < Count; iNote++)
            {
                int value = Tonic + range[Degree - 1 + iNote * 2];
                if (value > 127) break;
                Events.Add(new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn,
                    Value = value,
                    Delay = Delay + Arpeggio * iNote, // time to start playing the note
                    Channel = Channel,
                    Duration = Duration, // real duration. Set to -1 to indefinitely
                    Velocity = Velocity
                });
            }

            if (logChord)
            {
                string info = string.Format("Tonic:{0} Degree:{1}", HelperNoteLabel.LabelFromMidi(Tonic), Degree);
                foreach (MPTKEvent evnt in Events)
                    info += " " + HelperNoteLabel.LabelFromMidi(evnt.Value);
                Debug.Log(info);
            }
        }


        /// <summary>
        /// [MPTK PRO] Build a chord from the current chord in the lib ChordLib.csv in folder Resources/GeneratorTemplate.csv
        /// </summary>
        /// <param name="chordName">NAme of the chord</param>
        public void MPTK_BuildFromLib(MPTKChordName chordName)
        {
            MPTK_BuildFromLib((int)chordName);
        }

        /// <summary>
        /// [MPTK PRO] Build a chord
        /// </summary>
        /// <param name="pindex">position from 0 in ChordLib.csv</param>
        public void MPTK_BuildFromLib(int pindex)
        {
            int index = Mathf.Clamp(pindex, 0, MPTKChordLib.ChordCount - 1);
            MPTKChordLib chorLib = MPTKChordLib.Chords[index];

            Tonic = Mathf.Clamp(Tonic, 0, 127);
            Velocity = Mathf.Clamp(Velocity, 0, 127);
            Duration = Clamp(Duration, -1, 999999);
            Delay = Clamp(Delay, 0, 999999);
            Arpeggio = Clamp(Arpeggio, 0, 1000);

            Events = new List<MPTKEvent>();

            // Add each notes to compose the chord. 
            for (int iNote = 0; iNote < chorLib.Count; iNote++)
            {
                int value = Tonic + chorLib[iNote];
                Events.Add(new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn,
                    Value = value,
                    Delay = Delay + Arpeggio * iNote, // time to start playing the note
                    Channel = Channel,
                    Duration = Duration, // real duration. Set to -1 to indefinitely
                    Velocity = Velocity
                });
            }

            if (logChord)
            {
                string info = string.Format("Tonic:{0} Degree:{1}", HelperNoteLabel.LabelFromMidi(Tonic), Degree);
                foreach (MPTKEvent evnt in Events)
                    info += " " + HelperNoteLabel.LabelFromMidi(evnt.Value);
                Debug.Log(info);
            }
        }
    }
}
