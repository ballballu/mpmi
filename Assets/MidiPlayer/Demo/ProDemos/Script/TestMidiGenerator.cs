
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System;
using UnityEngine.Events;
using MPTK.NAudio.Midi;
using System.Linq;
using MidiPlayerTK;

namespace DemoMPTK

{
    /// <summary>
    /// Script for the prefab MidiFilePlayer. 
    /// Play a selected MIDI file. 
    /// List of Midi file must be defined with Midi Player Setup (menu tools).
    /// </summary>
    public class TestMidiGenerator : MonoBehaviour
    {
        float spaceH = 30f;
        float spaceV = 5f;
        public CustomStyle myStyle;
        DateTime startPlaying;

        void OnGUI()
        {
            if (!HelperDemo.CheckSFExists()) return;

            // Set custom Style. Good for background color 3E619800
            if (myStyle == null) myStyle = new CustomStyle();

            MainMenu.Display("Create a MIDI messages by Algo, Write to a MIDI file, Play", myStyle, "https://paxstellar.fr/class-midifilewriter2/");

            GUILayout.BeginVertical(myStyle.BacgDemos);
            GUILayout.Label("Write the generated notes to a Midi file and play with a MidiExternalPlay Prefab.", myStyle.TitleLabel2Centered);
            GUILayout.Label("or play the generated notes with MidiFilePlayer Prefab, no file is created.", myStyle.TitleLabel2Centered);

            GUILayout.Space(spaceV);

            GUIExample(1, "A very simple stream, 4 notes of 500 milliseconds played every 500 milliseconds:");
            GUIExample(2, "A very simple stream, 4 consecutives quarters played independantly of the tempo:");
            GUIExample(3, "A more complex one, Preset change, Tempo change, Pitch Wheel change, Modulation change:");
            GUIExample(4, "Generated Chord:");
            GUIExample(5, "Sandbox, make your trial!");

            // ----------------------
            // Button for stop playing and open folder
            // ----------------------
            GUILayout.BeginHorizontal(myStyle.BacgDemos);

            if (GUILayout.Button("Open the folder", GUILayout.Width(120), GUILayout.Height(40)))
                Application.OpenURL("file://" + Application.persistentDataPath);
            GUILayout.Space(spaceH);

            GUILayout.Label("Note: a low pass effect is defined with MidiExternalPlay prefab (button Write and Play) also it sound differently that MidiFilePlayer prefab (button Play Directly). See inspector.", myStyle.LabelLeft, GUILayout.Width(600), GUILayout.Height(40));

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void GUIExample(int number, string title)
        {
            int actionWanted = 0;
            MidiFileWriter2 mfw = null;

            GUILayout.BeginHorizontal(myStyle.BacgDemos1);
            GUILayout.Label($"Example {number}", myStyle.TitleLabel2, GUILayout.Width(100), GUILayout.Height(40));
            GUILayout.Label(title, myStyle.LabelLeft, GUILayout.Width(400), GUILayout.Height(40));

            if (GUILayout.Button("Write and Play", GUILayout.Width(100), GUILayout.Height(40)))
                actionWanted = 1;

            GUILayout.Space(spaceH);

            if (GUILayout.Button("Play Directly ", GUILayout.Width(100), GUILayout.Height(40)))
                actionWanted = 2;

            if (actionWanted != 0)
            {
                StopAllPlaying();
                switch (number)
                {
                    case 1: mfw = CreateMidiStream_1(); break;
                    case 2: mfw = CreateMidiStream_2(); break;
                    case 3: mfw = CreateMidiStream_3(); break;
                    case 4: mfw = CreateMidiStream_Chord(); break;
                    case 5: mfw = CreateMidiStream_sandbox(); break;
                }
                if (actionWanted == 1) WriteMidiSequenceToFileAndPlay($"Generated {number}", mfw);
                if (actionWanted == 2) PlayDirectlyMidiSequence($"Generated {number}", mfw);

            }
            GUILayout.Space(spaceH);
            if (GUILayout.Button("Stop playing", GUILayout.Width(100), GUILayout.Height(40)))
                StopAllPlaying();
            GUILayout.EndHorizontal();
            GUILayout.Space(spaceV);
        }

        /// <summary>
        /// Play four consecutive quarters from 60 (C5) to 63.
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_1()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2();

            mfw.MPTK_AddText(track0, 0, MPTKMeta.Copyright, "Simple Midi Generated. 4 quarter at 120 BPM");

            // Playing tempo must be defined at start of the stream. 
            // Defined BPM is mandatory when duration and delay are defined in millisecond in the stream. 
            // The value of the BPM is used to transform duration from milliseconds to internal ticks value.
            // Obviously, duration in millisecànds depends on the BPM selected. With BPM=120, a quarter duration is 500 milliseconds.
            mfw.MPTK_AddBPMChange(track0, 0, 120);

            // Add four consecutive quarters from 60 (C5)  to 63.
            // With BPM=120, quarter duration is 500ms (60000 / 120). So, notes are played at, 0, 500, 1000, 1500 ms from the start.
            mfw.MPTK_AddNoteMilli(track1, 200f, channel0, 48, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 800f, channel0, 52, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 1400f, channel0, 56, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 2000f, channel0, 60, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 2600f, channel0, 64, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 3200f, channel0, 68, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 3800f, channel0, 72, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 4400f, channel0, 76, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 5000f, channel0, 72, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 5600f, channel0, 68, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 6200f, channel0, 64, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 6800f, channel0, 60, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 7400f, channel0, 56, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 8000f, channel0, 52, 80, 1000f);
            mfw.MPTK_AddNoteMilli(track1, 8600f, channel0, 48, 80, 1000f);
            // Silent note : velocity=0 (will generate only a noteoff)


            return mfw;
        }

        /// <summary>
        /// Four consecutive quarters played independently of the tempo.
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_2()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            long absoluteTime = 0;


            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2();

            mfw.MPTK_AddTimeSignature(0, 0, 4, 4);

            // 240 is the default. A classical value for a Midi. define the time precision.
            int ticksPerQuarterNote = mfw.MPTK_DeltaTicksPerQuarterNote;

            // Some textual information added to the track 0 at time=0
            mfw.MPTK_AddText(track0, 0, MPTKMeta.Copyright, "Simple Midi Generated. 4 quarter at 120 BPM");

            // Define Tempo is not mandatory when using time in ticks. The default 120 BPM will be used.
            //mfw.MPTK_AddBPMChange(track0, 0, 120);

            // Add four consecutive quarters from 60 (C5)  to 63.
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 60, 50, ticksPerQuarterNote);

            // Next note will be played one quarter after the previous
            absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 61, 50, ticksPerQuarterNote);

            absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 62, 50, ticksPerQuarterNote);

            absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 63, 50, ticksPerQuarterNote);

            // Silent note : velocity=0 (will generate only a noteoff)
            absoluteTime += ticksPerQuarterNote * 2;
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 80, 0, ticksPerQuarterNote);

            return mfw;
        }

        /// <summary>
        /// Midi Generated with MPTK with tempo, preset, pitch wheel change
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_3()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            // https://paxstellar.fr/2020/09/11/midi-timing/
            int beatsPerMinute = 60;

            // a classical value for a Midi. define the time precision
            int ticksPerQuarterNote = 500;

            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2(ticksPerQuarterNote, 1);

            // Time to play a note expressed in ticks.
            // All durations are expressed in ticks, so this value can be used to convert
            // duration notes as quarter to ticks. https://paxstellar.fr/2020/09/11/midi-timing/
            // If ticksPerQuarterNote = 120 and absoluteTime = 120 then the note will be played a quarter delay from the start.
            // If ticksPerQuarterNote = 120 and absoluteTime = 1200 then the note will be played a 10 quarter delay from the start.
            long absoluteTime = 100;

            // Some textual information added to the track 0 at time=0
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Midi Generated with MPTK with tempo, preset, pitch wheel change");

            // TimeSignatureEvent (not mandatory)   https://paxstellar.fr/2020/09/11/midi-timing/
            //      Numerator(number of beats in a bar, 
            //      Denominator(which is confusingly) in 'beat units' so 1 means 2, 2 means 4(crochet), 3 means 8(quaver), 4 means 16 and 5 means 32), 
            mfw.MPTK_AddTimeSignature(track0, absoluteTime, 4, 2); // for a 4/4 signature

            // Tempo is defined in beat per minute (not mandatory).
            // beatsPerMinute set to 60 at start, it's a slow tempo, one quarter per second.
            // Tempo is global for the whole MIDI independantly of each track and channel.
            // Of course tempo can be changed more later.
            mfw.MPTK_AddBPMChange(track0, absoluteTime, beatsPerMinute);

            // Preset for channel 1. Generally 25 is Acoustic Guitar, see https://en.wikipedia.org/wiki/General_MIDI
            // It seems that some reader (as Media Player) refused Midi file if change preset is defined in the track 0, so we set it in track 1.
            mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 0);

            //
            // Build first bar
            // ---------------

            // Creation of the first bar in the partition : 
            //      add four notes with a duration of one quarter 
            //      and velocity at 50 at start with increase until 100
            //          57 --> A4   
            //          60 --> C5   
            //          62 --> D5  
            //          65 --> F5 

            // // Some lyrics added to the track 0
            // mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Build first bar");

            // mfw.MPTK_AddNote(track1, absoluteTime, channel0, 57, 50, ticksPerQuarterNote);
            // absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)

            // mfw.MPTK_AddNote(track1, absoluteTime, channel0, 60, 80, ticksPerQuarterNote);
            // absoluteTime += ticksPerQuarterNote;

            // mfw.MPTK_AddNote(track1, absoluteTime, channel0, 62, 100, ticksPerQuarterNote);
            // absoluteTime += ticksPerQuarterNote;

            // mfw.MPTK_AddNote(track1, absoluteTime, channel0, 65, 100, ticksPerQuarterNote);
            // absoluteTime += ticksPerQuarterNote;

            // //
            // // Build second bar : one note with a pitch change along the bar
            // // -------------------------------------------------------------

            // mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 50); // synth string

            // // Some lyrics added to the track 0
            // mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Pitch wheel effect");

            // // Play an infinite note A4 (duration = -1) don't forget the noteoff!
            // mfw.MPTK_AddNote(track1, absoluteTime, channel0, 57, 100, -1);

            // // Apply pitch wheel on the channel 0
            // for (float pitch = 0f; pitch <= 2f; pitch += 0.05f) // 40 steps of 0.05
            // {
            //     mfw.MPTK_AddPitchWheelChange(track1, absoluteTime, channel0, pitch);
            //     // Advance position 40 steps and for a total duration of 4 quarters
            //     absoluteTime += (long)((float)ticksPerQuarterNote * 4f / 40f);
            // }

            // // The noteoff for A4
            // mfw.MPTK_AddOff(track1, absoluteTime, channel0, 57);

            // // Reset pitch change to normal value
            // mfw.MPTK_AddPitchWheelChange(track1, absoluteTime, channel0, 0.5f);

            //
            // Build third bar : arpeggio of 16 sixteenth along the bar 
            // --------------------------------------------------------

            // Some lyrics added to the track 0
            // mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Arpeggio");

            // // Dobble the tempo with a variant of MPTK_AddBPMChange, 
            // // change tempo defined in microsecond. Use MPTK_BPM2MPQN to convert or use directly MPTK_AddBPMChange
            // mfw.MPTK_AddTempoChange(track0, absoluteTime, MidiFileWriter2.MPTK_BPM2MPQN(beatsPerMinute*2));

            // // Patch/preset to use for channel 1. Generally 11 is Music Box, see https://en.wikipedia.org/wiki/General_MIDI
            // mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 11);

            // // Add sixteenth notes (duration = quarter / 4) : need 16 sixteenth to build a bar of 4 quarter
            int note = 57;
            // for (int i = 0; i < 16; i++)
            // {
            //     mfw.MPTK_AddNote(track1, absoluteTime, channel0, note, 100, ticksPerQuarterNote / 4);
            //     // Advance the position by one sixteenth 
            //     absoluteTime += ticksPerQuarterNote / 4;
            //     note += 1;
            // }

            //
            // Build fourth bar : one whole note with vibrato
            // ----------------------------------------------

            // Some lyrics added to the track 0
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Vibrato");

            // Add a last whole note (4 quarters duration = 1 bar)
            //mfw.MPTK_AddNote(track1, absoluteTime, channel0, 85, 100, ticksPerQuarterNote * 4);

            // Apply modulation change, (vibrato)
            //mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.Pan, 127);
            //mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.PORTAMENTO_TIME_MSB, 80);
            mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.Portamento, 127);
            mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.PORTAMENTO_TIME_MSB, 50);
            for (int i = 0; i < 8; i++)
            {
                mfw.MPTK_AddNote(track1, absoluteTime, channel0, note, 100, ticksPerQuarterNote * 4);
                // Advance the position by one sixteenth 
                absoluteTime += ticksPerQuarterNote / 2;
                note += 4;
            }

            //absoluteTime += ticksPerQuarterNote * 4;
            absoluteTime += ticksPerQuarterNote*4;
            mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.Portamento, 0);
            // Reset modulation change to normal value
            // mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.Pan, 20);
            note = 57;
            for (int i = 0; i < 8; i++)
            {
                mfw.MPTK_AddNote(track1, absoluteTime, channel0, note, 100, ticksPerQuarterNote * 4);
                // Advance the position by one sixteenth 
                absoluteTime += ticksPerQuarterNote / 2;
                note += 4;
            }
            // absoluteTime += ticksPerQuarterNote*4;
            // mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.Pan, 64);
            // note = 57;
            // mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.Expression, 20);
            // for (int i = 0; i < 8; i++)
            // {
            //     mfw.MPTK_AddNote(track1, absoluteTime, channel0, note, 100, ticksPerQuarterNote * 4);
            //     // Advance the position by one sixteenth 
            //     absoluteTime += ticksPerQuarterNote / 2;
            //     note += 4;
            // }
            // //absoluteTime += ticksPerQuarterNote * 4;
            // absoluteTime += ticksPerQuarterNote*4;
            // // Reset modulation change to normal value
            // mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.Expression, 127);
            //
            // Build wrap up : add a silence
            // -----------------------------

            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Silence");
            // It's better to not stop the playing just after the last note. 
            // Add a silence, but silence does'nt exists in Midi, so add a note with velocity=0
            absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 60, 0, ticksPerQuarterNote);

            // Now useless, track ending is automatically done
            //mfw.MPTK_EndTrack(track0);
            //mfw.MPTK_EndTrack(track1);

            return mfw;
        }

        private MidiFileWriter2 CreateMidiStream_Chord()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            // https://paxstellar.fr/2020/09/11/midi-timing/
            int beatsPerMinute = 60; // One quarter per second

            // a classical value for a Midi. define the time precision
            int ticksPerQuarterNote = 500;

            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2(ticksPerQuarterNote, 1);

            // Time to play a note expressed in ticks.
            // All durations are expressed in ticks, so this value can be used to convert
            // duration notes as quarter to ticks. https://paxstellar.fr/2020/09/11/midi-timing/
            // If ticksPerQuarterNote = 120 and absoluteTime = 120 then the note will be played a quarter delay from the start.
            // If ticksPerQuarterNote = 120 and absoluteTime = 1200 then the note will be played a 10 quarter delay from the start.
            long absoluteTime = 0;

            // Patch/preset to use for channel 1. Generally 40 is violin, see https://en.wikipedia.org/wiki/General_MIDI and substract 1 as preset begin at 0 in MPTK
            mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 40);

            mfw.MPTK_AddBPMChange(track0, absoluteTime, beatsPerMinute);

            // Some textual information added to the track 0 at time=0
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play chords");

            // Defined a duration of one quarter in millisecond
            long duration = (long)mfw.MPTK_ConvertTickToMilli(ticksPerQuarterNote);


            // From https://apprendre-le-home-studio.fr/bien-demarrer-ta-composition-46-suites-daccords-danthologie-a-tester-absolument-11-idees-de-variations/ (sorry, in french but it's more a note for me !)

            //! [ExampleMidiWriterBuildChordFromRange]

            // Play 4 chords, degree I - V - IV - V 
            // ------------------------------------
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play 4 chords, degree I - V - IV - V ");

            // We need degrees in major, so build a major range
            MPTKRangeLib rangeMajor = MPTKRangeLib.Range(MPTKRangeName.MajorHarmonic);

            // Build chord degree 1
            MPTKChordBuilder chordDegreeI = new MPTKChordBuilder()
            {
                // Parameters to build the chord
                Tonic = 60, // play in C
                Count = 3,  // 3 notes to build the chord (between 2 and 20, of course it doesn't make sense more than 7, its only for fun or experiementation ...)
                Degree = 1,
                // Midi Parameters how to play the chord
                Duration = duration, // millisecond, -1 to play indefinitely
                Velocity = 80, // Sound can vary depending on the velocity

                // Optionnal MPTK specific parameters
                Arpeggio = 0, // delay in milliseconds between each notes of the chord
                Delay = 0, // delay in milliseconds before playing the chord
            };

            // Build chord degree V
            MPTKChordBuilder chordDegreeV = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 5, Duration = duration, Velocity = 80, };
            
            // Build chord degree IV
            MPTKChordBuilder chordDegreeIV = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 4, Duration = duration, Velocity = 80, };

            // Add degrees I - V - IV - V in the MIDI (all in major) 
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeI); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeV); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeIV); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeV); absoluteTime += ticksPerQuarterNote;

            //! [ExampleMidiWriterBuildChordFromRange]

            // Add a silent
            absoluteTime += ticksPerQuarterNote;

            // Play 4 others chords, degree  I – VIm – IIm – V
            // -----------------------------------------------
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play 4 chords, degree I – VIm – IIm – V");

            // We need 2 degrees in minor, build a minor range
            MPTKRangeLib rangeMinor = MPTKRangeLib.Range(MPTKRangeName.MinorHarmonic);

            // then degree 2 and 6
            MPTKChordBuilder chordDegreeII = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 2, Duration = duration, Velocity = 80, };
            MPTKChordBuilder chordDegreeVI = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 6, Duration = duration, Velocity = 80, };

            // Add degrees I – VIm – IIm – V intp the MidiFileWriter2 MIDI stream
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeI); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMinor, chordDegreeVI); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMinor, chordDegreeII); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeV); absoluteTime += ticksPerQuarterNote;

            // Add a silent
            absoluteTime += ticksPerQuarterNote;


            // Play 4 chords from library
            // --------------------------
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play 4 chords from library");

            // Piano
            mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 0);

            //! [ExampleMidiWriterBuildChordFromLib]

            MPTKChordBuilder chordLib = new MPTKChordBuilder() { Tonic = 60, Duration = duration, Velocity = 80, };
            mfw.MPTK_AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.Major, chordLib); absoluteTime += ticksPerQuarterNote;
            chordLib.Tonic = 62;
            mfw.MPTK_AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.mM7, chordLib); absoluteTime += ticksPerQuarterNote;
            chordLib.Tonic = 67;
            mfw.MPTK_AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.m7b5, chordLib); absoluteTime += ticksPerQuarterNote;
            chordLib.Tonic = 65;
            mfw.MPTK_AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.M7, chordLib); absoluteTime += ticksPerQuarterNote;

            //! [ExampleMidiWriterBuildChordFromLib]

            // Return a MidiFileWriter2 object to be played or write
            // see PlayDirectlyMidiSequence() or WriteMidiSequenceToFileAndPlay ()
            return mfw;
        }


        /// <summary>
        /// Play four consecutive quarters from 60 (C5) to 63.
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_sandbox()
        {
            //In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Track is interesting to structure your Midi. It will be more readable on a sequencer. 
            // Also, track has no effect on the music, must not be confused with channel!
            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            // Track management is done automatically, they are created and ended when needed. There is no real limit, but this class limit the count to 64
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 111;

            int channel0 = 0; // we are using only one channel in this demo
            int channel1 = 1; // we are using only one channel in this demo

            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2();

            // Some textual information added to the track 0 at time=0
            mfw.MPTK_AddText(track0, 0, MPTKMeta.SequenceTrackName, "Sandbox");
            mfw.MPTK_AddChangePreset(track1, 0, channel0, 65); // alto sax
            mfw.MPTK_AddChangePreset(track1, 0, channel1, 18); // rock organ

            mfw.MPTK_AddBPMChange(track0, 0, 120);

            mfw.MPTK_AddTextMilli(track0, 3000f, MPTKMeta.TextEvent, "Alto Sax, please");
            mfw.MPTK_AddNoteMilli(track1, 1000f, channel0, 62, 50, -1);
            mfw.MPTK_AddOffMilli(track1, 4000f, channel0, 62);

            mfw.MPTK_AddNoteMilli(track1, 10, channel0, 60, 50, -1);
            mfw.MPTK_AddOffMilli(track1, 3000, channel0, 60);

            mfw.MPTK_AddTextMilli(track0, 3000f, MPTKMeta.TextEvent, "Rock Organ, please");
            mfw.MPTK_AddNoteMilli(track1, 3000f, channel1, 65, 50, 3000);
            mfw.MPTK_AddNoteMilli(track1, 3500f, channel1, 66, 50, 2500);
            mfw.MPTK_AddNoteMilli(track1, 4000f, channel1, 67, 50, 2000);


            mfw.MPTK_AddNoteMilli(track1, 1000f, channel1, 62, 50, -1);
            mfw.MPTK_AddOffMilli(track1, 4000f, channel1, 62);

            mfw.MPTK_AddTextMilli(track0, 6000f, MPTKMeta.TextEvent, "Ending Bip");

            mfw.MPTK_AddNoteMilli(track1, 6000f, channel0, 80, 50, 100f);
            return mfw;
        }

        private void PlayDirectlyMidiSequence(string name, MidiFileWriter2 mfw)
        {
            // Play MIDI with the MidiExternalPlay prefab without saving MIDI in a file
            MidiFilePlayer midiPlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiPlayer == null)
            {
                Debug.LogWarning("Can't find a MidiFilePlayer in the Hierarchy. No music will be played");
                return;
            }

            midiPlayer.MPTK_Stop();
            mfw.MPTK_MidiName = name;

            midiPlayer.OnEventStartPlayMidi.RemoveAllListeners();
            midiPlayer.OnEventStartPlayMidi.AddListener((string midiname) =>
            {
                startPlaying = DateTime.Now;
                Debug.Log($"Start playing {midiname} at {startPlaying}");
            });

            midiPlayer.OnEventEndPlayMidi.RemoveAllListeners();
            midiPlayer.OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) =>
            {
                Debug.Log($"End playing {midiname} {reason} Duration={(DateTime.Now - startPlaying).TotalSeconds:F3}");
            });

            midiPlayer.OnEventNotesMidi.RemoveAllListeners();
            midiPlayer.OnEventNotesMidi.AddListener((List<MPTKEvent> events) =>
            {
                foreach (MPTKEvent midievent in events)
                    Debug.Log($"At {midievent.RealTime:F1} ms play: {midievent.ToString()}");
            });


            //  midiPlayer.MPTK_Loop = true;

            // Sort the events by ascending absolute time (optional)
            mfw.MPTK_SortEvents();
            mfw.MPTK_Debug();

            // Send the MIDI sequence to the internal MIDI sequencer
            // -----------------------------------------------------
            Debug.Log("---------------- here0 ");
            midiPlayer.MPTK_Play(mfw);
        }

        private void WriteMidiSequenceToFileAndPlay(string name, MidiFileWriter2 mfw)
        {
            // build the path + filename to the midi
            string filename = Path.Combine(Application.persistentDataPath, name + ".mid");
            Debug.Log("Write Midi file:" + filename);

            // Sort the events by ascending absolute time (optional)
            mfw.MPTK_SortEvents();
            mfw.MPTK_Debug();

            // Write the MIDI file
            mfw.MPTK_WriteToFile(filename);

            // Need an external player to play MIDI from a file from a folder
            MidiExternalPlayer midiExternalPlayer = FindObjectOfType<MidiExternalPlayer>();
            if (midiExternalPlayer == null)
            {
                Debug.LogWarning("Can't find a MidiExternalPlayer in the Hierarchy. No music will be played");
                return;
            }
            midiExternalPlayer.MPTK_Stop();

            // this prefab is able to load a MIDI file from the device or from an url (http)
            // -----------------------------------------------------------------------------
            midiExternalPlayer.MPTK_MidiName = "file://" + filename;

            midiExternalPlayer.OnEventStartPlayMidi.RemoveAllListeners();
            midiExternalPlayer.OnEventStartPlayMidi.AddListener((string midiname) => { Debug.Log($"Start playing {midiname}"); });

            midiExternalPlayer.OnEventEndPlayMidi.RemoveAllListeners();
            midiExternalPlayer.OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) => { Debug.Log($"End playing {midiname} {reason}"); });

            midiExternalPlayer.MPTK_Loop = true;

            midiExternalPlayer.MPTK_Play();
        }

        private static void StopAllPlaying()
        {
            MidiExternalPlayer midiExternalPlayer = FindObjectOfType<MidiExternalPlayer>();
            if (midiExternalPlayer != null)
                midiExternalPlayer.MPTK_Stop();
            MidiFilePlayer midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiFilePlayer != null)
                midiFilePlayer.MPTK_Stop();
        }
    }
}

