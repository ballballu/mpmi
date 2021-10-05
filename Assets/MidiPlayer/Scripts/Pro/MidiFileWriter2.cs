using MPTK.NAudio.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// [MPTK PRO] - Write a midi file from differents sources based on NAudio frawemork.\n
    /// See full example TestMidiWriter.cs with a light sequencer.\n 
    /// This class replaced MidiFileWriter with these changes: channel start at 0, new specfic event, better control.\n 
    /// More information here: https://paxstellar.fr/class-midifilewriter2/
    /// </summary>
    public class MidiFileWriter2
    {
        /// <summary>
        /// From Midi Header: Delta Ticks Per Quarter Note. 
        /// Represent the duration time in "ticks" which make up a quarter-note. 
        /// For instance, if 96, then a duration of an eighth-note in the file would be 48.
        /// </summary>
        public int MPTK_DeltaTicksPerQuarterNote;

        private int _bpm;
        /// <summary>
        /// Get current Beats Per Minute  https://en.wikipedia.org/wiki/Tempo
        /// </summary>
        public int Bpm
        {
            get { return _bpm; }
        }

        private int _microsecondsPerQuaterNote;
        /// <summary>
        /// Get current Microseconds Per Quater Note https://en.wikipedia.org/wiki/Tempo
        /// The tempo is given in micro seconds per quarter beat. To convert this to BPM use method MPTK_MPQN2BPM
        /// This value can change during the playing when a change tempo event is defined. See here for more information https://paxstellar.fr/2020/09/11/midi-timing/        
        /// </summary>
        public int MPTK_MicrosecondsPerQuaterNote
        {
            get { return _microsecondsPerQuaterNote; }
        }

        /// <summary>
        /// DEPRECATED - use MPTK_BPM2MPQN in place - Convert BPM to duration of a quarter in microsecond
        /// </summary>
        /// <param name="bpm">m</param>
        /// <returns></returns>
        public static int MPTK_GetMicrosecondsPerQuaterNote(int bpm)
        {
            Debug.LogWarning("MPTK_GetMicrosecondsPerQuaterNote is deprecated, use MPTK_BPM2MPQN in place");
            return 60 * 1000 * 1000 / bpm;
        }

        /// <summary>
        /// Convert BPM to duration of a quarter in microsecond
        /// </summary>
        /// <param name="bpm">m</param>
        /// <returns></returns>
        public static int MPTK_BPM2MPQN(int bpm)
        {
            return 60000000 / bpm;
        }

        /// <summary>
        /// Convert duration of a quarter in microsecond to Beats Per Minute
        /// </summary>
        /// <param name="microsecondsPerQuaterNote"></param>
        /// <returns></returns>
        public static int MPTK_MPQN2BPM(int microsecondsPerQuaterNote)
        {
            return 60000000 / microsecondsPerQuaterNote;
        }

        /// <summary>
        /// Lenght in millisecond of a tick. Obviously depends on the current tempo and the ticks per quarter.
        /// </summary>
        public float MPTK_PulseLenght
        {
            get
            {
                return (60000 / _bpm) / MPTK_DeltaTicksPerQuarterNote; /* in milliseconds */
            }
        }

        /// <summary>
        /// Convert the tick duration to a real time duration in millisecond regarding the current tempo.
        /// </summary>
        /// <param name="tick">duration in ticks</param>
        /// <returns>duration in milliseconds</returns>
        public float MPTK_ConvertTickToMilli(long tick)
        {
            return tick * MPTK_PulseLenght;
        }

        /// <summary>
        /// Convert a real time duration in millisecond to a number of tick regarding the current tempo.
        /// </summary>
        /// <param name="time">duration in milliseconds</param>
        /// <returns>duration in ticks</returns>
        public long MPTK_ConvertMilliToTick(float time)
        {
            if (MPTK_PulseLenght != 0d)
                return Convert.ToInt64((time / MPTK_PulseLenght) + 0.5f);
            else
                return 0;
        }

        /// <summary>
        /// Get the count of track.
        /// Track management is done automatically, they are created and ended as required. 
        /// The count of tracks is limited to 64.
        /// </summary>
        public int MPTK_TrackCount { get { return trackCount; } }

        private int trackCount;

        /// <summary>
        /// Get the midi file type of the loaded midi (0,1,2)
        /// </summary>
        public int MPTK_MidiFileType;

        /// <summary>
        /// Name of this midi stream
        /// </summary>
        public string MPTK_MidiName;

        public List<TrackMidiEvent> TmidiEvents;

        public int MPTK_CountEvent
        {
            get { return TmidiEvents == null ? 0 : TmidiEvents.Count; }
        }

        /// <summary>
        /// Create an empty MidiFileWriter2. Set default value from the midi norm:
        ///    - type = 1 
        ///    - Delta Ticks Per Quarter Note = 240 
        ///    - Beats Per Minute = 120
        /// </summary>
        public MidiFileWriter2()
        {
            try
            {
                TmidiEvents = new List<TrackMidiEvent>();
                MPTK_DeltaTicksPerQuarterNote = 240;
                MPTK_MidiFileType = 1;
                _bpm = 120;
                _microsecondsPerQuaterNote = MPTK_BPM2MPQN(_bpm);
                trackCount = 0;
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Create a MidiFileWriter2 with specific header midi value (for advanced use)
        /// </summary>
        /// <param name="deltaTicksPerQuarterNote">Delta Ticks Per Quarter Note</param>
        /// <param name="midiFileType">type of Midi format. Must be 0 or 1 (better)</param>
        public MidiFileWriter2(int deltaTicksPerQuarterNote, int midiFileType)
        {
            try
            {
                TmidiEvents = new List<TrackMidiEvent>();
                MPTK_DeltaTicksPerQuarterNote = deltaTicksPerQuarterNote;
                MPTK_MidiFileType = midiFileType;
                _bpm = 120;
                _microsecondsPerQuaterNote = MPTK_BPM2MPQN(_bpm);
                trackCount = 0;
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Remove all midi events
        /// </summary>
        public void MPTK_Clear()
        {
            trackCount = 0;
            TmidiEvents.Clear();
        }
        /// <summary>
        /// Create a MidiFileWriter2 from a MidiFilePlayer. A midi file must be loaded before from a MidiFilePlayer gameobject (as in example) 
        /// or from a call to MidiFileWriter2.MPTK_LoadFromFile(filename).
        /// @code
        ///        MidiFilePlayer mfp = FindObjectOfType<MidiFilePlayer>();
        ///        if (mfp != null)
        ///        {
        ///            if (mfp.MPTK_IsPlaying)
        ///            {
        ///                 string filename = Path.Combine(Application.persistentDataPath, mfp.MPTK_MidiName + ".mid");
        ///                 MidiFileWriter2 mfw = new MidiFileWriter2(mfp.MPTK_DeltaTicksPerQuarterNote, 1);
        ///                 mfw.MPTK_LoadFromMPTK(mfp.MPTK_MidiEvents, mfp.MPTK_TrackCount);
        ///                 mfw.MPTK_WriteToFile(filename);
        ///             }
        ///            else
        ///                 Debug.LogWarning("MidiFilePlayer must playing to export midi data");
        ///        }
        ///        else
        ///             Debug.LogWarning("No MidiFilePLayer found in the hierarchy");
        /// @endcode
        /// </summary>
        /// <param name="midiEvents">List of TrackMidiEvent</param>
        /// <param name="track">TRack count, default 1</param>
        public bool MPTK_LoadFromMPTK(List<TrackMidiEvent> midiEvents, int track = 1)
        {
            bool ok = false;
            try
            {
                TmidiEvents = midiEvents;
                trackCount = track;
                ok = true;
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>
        /// Create a MidiFileWriter2 from a Midi found in MPTK MidiDB
        /// @code
        ///     // Create a midi file writer
        ///     MidiFileWriter2 mfw = new MidiFileWriter2();
        ///     // Load the selected midi from MidiDB index
        ///     mfw.MPTK_LoadFromMidiDB(selectedMidi);
        ///     // build th path + filename to the midi
        ///     string filename = Path.Combine(Application.persistentDataPath, MidiPlayerGlobal.CurrentMidiSet.MidiFiles[selectedMidi] + ".mid");
        ///     // write the midi file
        ///     mfw.MPTK_WriteToFile(filename);
        /// @endcode
        /// </summary>
        /// <param name="indexMidiDb"></param>
        public bool MPTK_LoadFromMidiDB(int indexMidiDb)
        {
            bool ok = false;
            try
            {
                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                {
                    if (indexMidiDb >= 0 && indexMidiDb < MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count - 1)
                    {
                        string midiname = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[indexMidiDb];
                        TextAsset mididata = Resources.Load<TextAsset>(Path.Combine(MidiPlayerGlobal.MidiFilesDB, midiname));
                        MidiLoad midiLoad = new MidiLoad();
                        midiLoad.KeepNoteOff = true;
                        midiLoad.MPTK_Load(mididata.bytes);
                        TmidiEvents = midiLoad.MPTK_MidiEvents;
                        trackCount = midiLoad.MPTK_TrackCount;
                        ok = true;
                    }
                    else
                        Debug.LogWarning("Index is out of the MidiDb list");
                }
                else
                    Debug.LogWarning("No MidiDb defined");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>
        /// DEPRECATED Create more tracks in the stream (one default track is created at init)
        /// From version 2.88 tracks will be created automatically when writing Midi to a file. See MPTK_WriteToFile.
        /// </summary>
        /// <param name="count">number of tracks to create</param>
        public void MPTK_CreateTrack(int count)
        {
            Debug.LogWarning("The method MPTK_CreateTrack is deprecated. Tracks are automatically created");
            //try
            //{
            //    trackCount += count;
            //}
            //catch (System.Exception ex)
            //{
            //    MidiPlayerGlobal.ErrorDetail(ex);
            //}
        }

        /// <summary>
        /// DEPRECATED Close the track (mandatory for a well formed midi file).
        /// From version 2.88 tracks will be ended automatically when writing Midi to a file. See MPTK_WriteToFile.
        /// </summary>
        /// <param name="trackNumber">Track number to close</param>
        public void MPTK_EndTrack(int trackNumber)
        {
            Debug.LogWarning("The method MPTK_EndTrack is deprecated. Tracks will be automatically ended when the midi is writed");
            //try
            //{
            //    long endLastEvent = 0;
            //    if (MidiEvents[trackNumber].Count > 0)
            //    {
            //        // if no noteon found, get time of the last event
            //        endLastEvent = MidiEvents[trackNumber][MidiEvents[trackNumber].Count - 1].AbsoluteTime;

            //        // search the last noteon event
            //        for (int index = MidiEvents[trackNumber].Count - 1; index >= 0; index--)
            //        {
            //            if (MidiEvents[trackNumber][index] is NoteOnEvent)
            //            {
            //                NoteOnEvent lastnoteon = (NoteOnEvent)MidiEvents[trackNumber][index];
            //                endLastEvent = lastnoteon.AbsoluteTime + lastnoteon.NoteLength;
            //                //Debug.Log("lastnoteon " + lastnoteon.NoteName);
            //                break;
            //            }
            //        }
            //    }
            //    //Debug.Log("Close track at " + endLastEvent);
            //    MidiEvents[trackNumber].Add(new MetaEvent(MetaEventType.EndTrack, 0, endLastEvent));
            //}
            //catch (System.Exception ex)
            //{
            //    MidiPlayerGlobal.ErrorDetail(ex);
            //}
        }

        // Internal class       
        private void AddEvent(int track, MidiEvent midievent)
        {
            track = CheckParam(track);

            try
            {
                TmidiEvents.Add(new TrackMidiEvent() { Event = midievent, IndexTrack = track });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private int CheckParam(int track)
        {
            if (TmidiEvents == null)
                throw new Exception("MidiEvents reference not set to an instance of object");

            if (track < 0)
            {
                Debug.LogWarning($"The number of track ({track}) cannnot be negative, track forced to 0");
                track = 0;
            }
            else if (track >= 64)
            {
                Debug.LogWarning($"The number of track ({track}) cannnot be more than 64, track forced to 63");
                track = 63;
            }
            if (track >= trackCount)
                trackCount = track + 1;
            return track;
        }

        /// <summary>
        ///Add a note on event at a specific time in millisecond The corresponding Noteoff is automatically created if duration > 0
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="timeToPlay">time in millisecond to play the note from the start of the Midi</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        /// <param name="velocity">Velocity must be in the range 0-127.</param>
        /// <param name="duration">Duration in millisecond. No noteoff is added if duration is <= 0, need to be added with MPTK_AddOffMilli</param>
        public void MPTK_AddNoteMilli(int track, float timeToPlay, int channel, int note, int velocity, float duration)
        {
            try
            {
                long absoluteTime = MPTK_ConvertMilliToTick(timeToPlay);
                int durationNote = duration <= 0 ? -1 : (int)MPTK_ConvertMilliToTick(duration);
                MPTK_AddNote(track, absoluteTime, channel, note, velocity, durationNote);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a note on event at an absolute time (tick count). The corresponding Noteoff is automatically created if duration > 0
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        /// <param name="velocity">Velocity must be in the range 0-127.</param>
        /// <param name="duration">Tick duration. No noteoff is added if duration is <= 0, need to be added with MPTK_AddOff</param>
        public void MPTK_AddNote(int track, long absoluteTime, int channel, int note, int velocity, int duration)
        {
            try
            {
                if (duration <= 0)
                    // duration not specifed, set a default of a quarter. A next note off event will whange this duration.
                    AddEvent(track, new NoteOnEvent(absoluteTime, channel + 1, note, velocity, MPTK_DeltaTicksPerQuarterNote));
                else
                {
                    AddEvent(track, new NoteOnEvent(absoluteTime, channel + 1, note, velocity, duration));
                    AddEvent(track, new NoteEvent(absoluteTime + duration, channel + 1, MidiCommandCode.NoteOff, note, 0));
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        ///Add a note off event at a specific time in millisecond The corresponding Noteoff is automatically created if duration > 0
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="timeToPlay">time in millisecond to play the note from the start of the Midi</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        public void MPTK_AddOffMilli(int track, float timeToPlay, int channel, int note)
        {
            try
            {
                long absoluteTime = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddOff(track, absoluteTime, channel, note);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a note off event. 
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        public void MPTK_AddOff(int track, long absoluteTime, int channel, int note)
        {
            try
            {
                track = CheckParam(track);

                int index = TmidiEvents.Count - 1;

                while (index >= 0)
                {
                    if (TmidiEvents[index].IndexTrack == track &&
                        TmidiEvents[index].Event.CommandCode == MidiCommandCode.NoteOn &&
                        TmidiEvents[index].Event.Channel == channel + 1 &&
                        ((NoteOnEvent)TmidiEvents[index].Event).NoteNumber == note)
                    {
                        ((NoteOnEvent)TmidiEvents[index].Event).OffEvent.AbsoluteTime = absoluteTime;
                        AddEvent(track, new NoteEvent(absoluteTime, channel + 1, MidiCommandCode.NoteOff, note, 0));
                        //int duration = (int)(absoluteTime - TmidiEvents[index].Event.AbsoluteTime);
                        //NoteOnEvent midievent = new NoteOnEvent(TmidiEvents[index].Event.AbsoluteTime, channel + 1, note, ((NoteOnEvent)TmidiEvents[index].Event).Velocity, duration);
                        //TmidiEvents[index] = new TrackMidiEvent() { Event = midievent, IndexTrack = track };
                        break;
                    }
                    index--;
                }
                if (index < 0)
                    Debug.LogWarning($"NoteOn {note} not found for NoteOff at {absoluteTime}");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a chord from a range
        /// @snippet TestMidiGenerator.cs ExampleMidiWriterBuildChordFromRange
        /// </summary>
        /// <param name="track"></param>
        /// <param name="absoluteTime"></param>
        /// <param name="channel"></param>
        /// <param name="range">See MPTKRangeLib</param>
        /// <param name="chord">See MPTKChordBuilder</param>
        public void MPTK_AddChordFromRange(int track, long absoluteTime, int channel, MPTKRangeLib range, MPTKChordBuilder chord)
        {
            try
            {
                chord.MPTK_BuildFromRange(range);
                foreach (MPTKEvent evnt in chord.Events)
                    MPTK_AddNote(track, absoluteTime, channel, evnt.Value, evnt.Velocity, (int)MPTK_ConvertMilliToTick(evnt.Duration));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a chord from a library of chord
        /// @snippet TestMidiGenerator.cs ExampleMidiWriterBuildChordFromLib
        /// </summary>
        /// <param name="track"></param>
        /// <param name="absoluteTime"></param>
        /// <param name="channel"></param>
        /// <param name="chordName">Name of the chord See #MPTKChordName</param>
        /// <param name="chord">See MPTKChordBuilder</param>
        public void MPTK_AddChordFromLib(int track, long absoluteTime, int channel, MPTKChordName chordName, MPTKChordBuilder chord)
        {
            try
            {
                chord.MPTK_BuildFromLib(chordName);
                foreach (MPTKEvent evnt in chord.Events)
                    MPTK_AddNote(track, absoluteTime, channel, evnt.Value, evnt.Velocity, (int)MPTK_ConvertMilliToTick(evnt.Duration));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a change preset
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="timeToPlay">time in millisecond to play the note from the start of the Midi</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="preset">Preset (program/patch) must be in the range 0-127</param>
        public void MPTK_AddChangePresetMilli(int track, float timeToPlay, int channel, int preset)
        {
            try
            {
                long absoluteTime = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddChangePreset(track, absoluteTime, channel, preset);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a change preset
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="preset">Preset (program/patch) must be in the range 0-127</param>
        public void MPTK_AddChangePreset(int track, long absoluteTime, int channel, int preset)
        {
            try
            {
                AddEvent(track, new PatchChangeEvent(absoluteTime, channel + 1, preset));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a Channel After-Touch Event
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="timeToPlay">time in millisecond to play the note from the start of the Midi</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="afterTouchPressure">After-touch pressure from 0 to 127</param>
        public void MPTK_AddChannelAfterTouchMilli(int track, float timeToPlay, int channel, int afterTouchPressure)
        {
            try
            {
                long absoluteTime = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddChannelAfterTouch(track, absoluteTime, channel, afterTouchPressure);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a Channel After-Touch Event
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="afterTouchPressure">After-touch pressure from 0 to 127</param>
        public void MPTK_AddChannelAfterTouch(int track, long absoluteTime, int channel, int afterTouchPressure)
        {
            try
            {
                AddEvent(track, new ChannelAfterTouchEvent(absoluteTime, channel + 1, afterTouchPressure));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Creates a control change event
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="timeToPlay">time in millisecond to play the note from the start of the Midi</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="controller">The MIDI Controller. See #MPTKController</param>
        /// <param name="controllerValue">Controller value</param>
        public void MPTK_AddControlChangeMilli(int track, float timeToPlay, int channel, MPTKController controller, int controllerValue)
        {
            try
            {
                long absoluteTime = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddControlChange(track, absoluteTime, channel, controller, controllerValue);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Creates a general control change event (CC)
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="controller">The MIDI Controller. See #MPTKController</param>
        /// <param name="controllerValue">Controller value</param>
        public void MPTK_AddControlChange(int track, long absoluteTime, int channel, MPTKController controller, int controllerValue)
        {
            try
            {
                AddEvent(track, new ControlChangeEvent(absoluteTime, channel + 1, (MidiController)controller, controllerValue));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Creates a control change event (CC) for the pitch (Pitch Wheel)\n
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="timeToPlay">time in millisecond to play the note from the start of the Midi</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="pitchWheel">Pitch Wheel Value. 1:normal value, 0:pitch mini, 2:pitch max</param>
        public void MPTK_AddPitchWheelChangeMilli(int track, float timeToPlay, int channel, float pitchWheel)
        {
            try
            {
                long absoluteTime = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddPitchWheelChange(track, absoluteTime, channel, pitchWheel);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Creates a control change event (CC) for the pitch (Pitch Wheel)\n
        /// pitchWheel=
        ///! @li @c  0      minimum (0 also for midi standard event value) 
        ///! @li @c  0.5    centered value (8192 for midi standard event value) 
        ///! @li @c  1      maximum (16383 for midi standard event value)
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="pitchWheel">Normalized Pitch Wheel Value. Range 0 to 1. V2.88.2 range normalized from 0 to 1.</param>
        public void MPTK_AddPitchWheelChange(int track, long absoluteTime, int channel, float pitchWheel)
        {
            try
            {
                int pitch = (int)Mathf.Lerp(0f, 16383f, pitchWheel); // V2.88.2 range normalized from 0 to 1
                                                                     //Debug.Log($"{pitchWheel} --> {pitch}");
                AddEvent(track, new PitchWheelChangeEvent(absoluteTime, channel + 1, pitch));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a tempo change to the midi stream. There is no channel in parameter because tempo change is apply to the whole midi.
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Tick time for this event</param>
        /// <param name="bpm">quarter per minute</param>
        public void MPTK_AddBPMChange(int track, long absoluteTime, int bpm)
        {
            try
            {
                _bpm = bpm;
                _microsecondsPerQuaterNote = MPTK_BPM2MPQN(bpm);
                AddEvent(track, new TempoEvent(_microsecondsPerQuaterNote, absoluteTime));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Add a tempo change to the midi stream in microseconds per quarter note. 
        /// There is no channel in parameter because tempo change apply to the whole midi.
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Tick time for this event</param>
        /// <param name="microsecondsPerQuarterNote">Microseconds per quarter note</param>
        public void MPTK_AddTempoChange(int track, long absoluteTime, int microsecondsPerQuarterNote)
        {
            try
            {
                _microsecondsPerQuaterNote = microsecondsPerQuarterNote;
                _bpm = MPTK_MPQN2BPM(microsecondsPerQuarterNote);
                AddEvent(track, new TempoEvent(microsecondsPerQuarterNote, absoluteTime));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Create a new TimeSignatureEvent. This event is optionnal. 
        /// Internal Midi sequencer assumes the default value is 4,4,24,32.  No track nor channel as teampo change applied to the whole midi.
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Time at which to create this event</param>
        /// <param name="numerator">Numerator</param>
        /// <param name="denominator">Denominator</param>
        /// <param name="ticksInMetronomeClick">Ticks in Metronome Click. Set to 24 for a standard value.</param>
        /// <param name="no32ndNotesInQuarterNote">No of 32nd Notes in Quarter Click. Set to 32 for a standard value.</param>
        /// More info here https://paxstellar.fr/2020/09/11/midi-timing/
        public void MPTK_AddTimeSignature(int track, long absoluteTime, int numerator = 4, int denominator = 4, int ticksInMetronomeClick = 24, int no32ndNotesInQuarterNote = 32)
        {
            try
            {
                AddEvent(track, new TimeSignatureEvent(absoluteTime, numerator, denominator, ticksInMetronomeClick, no32ndNotesInQuarterNote));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Create a new TimeSignatureEvent. This event is optionnal. Midi sequencer assumes the default value is 4,4,24,32.  No track nor channel as teampo change applied to the whole midi.
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="timeToPlay">time in millisecond to play the note from the start of the Midi</param>
        /// <param name="typeMeta">MetaEvent type (must be one that is
        /// <param name="text">The text in this type</param>
        /// associated with text data)</param>
        public void MPTK_AddTextMilli(int track, float timeToPlay, MPTKMeta typeMeta, string text)
        {
            try
            {
                long absoluteTime = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddText(track, absoluteTime, typeMeta, text);
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Create a new TimeSignatureEvent. This event is optionnal. Midi sequencer assumes the default value is 4,4,24,32.  No track nor channel as teampo change applied to the whole midi.
        /// </summary>
        /// <param name="track">Track for this event</param>
        /// <param name="absoluteTime">Absolute time of this event</param>
        /// <param name="typeMeta">MetaEvent type (must be one that is
        /// <param name="text">The text in this type</param>
        /// associated with text data)</param>
        public void MPTK_AddText(int track, long absoluteTime, MPTKMeta typeMeta, string text)
        {
            try
            {
                switch (typeMeta)
                {
                    case MPTKMeta.TextEvent:
                    case MPTKMeta.Copyright:
                    case MPTKMeta.DeviceName:
                    case MPTKMeta.Lyric:
                    case MPTKMeta.ProgramName:
                    case MPTKMeta.SequenceTrackName:
                    case MPTKMeta.Marker:
                    case MPTKMeta.TrackInstrumentName:
                        AddEvent(track, new TextEvent(text, (MetaEventType)typeMeta, absoluteTime));
                        break;
                    default:
                        throw new Exception($"MPTK_AddText need a meta event type for text. {typeMeta} is not correct.");
                }
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }


        /// <summary>
        /// Load a Midi file from OS system file (could be dependant of the OS)
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool MPTK_LoadFromFile(string filename)
        {
            bool ok = false;
            try
            {
                MidiLoad midiLoad = new MidiLoad();
                midiLoad.KeepNoteOff = true;
                if (midiLoad.MPTK_Load(filename))
                {
                    //MidiEvents = midiLoad.midifile.Events;
                    TmidiEvents = midiLoad.MPTK_MidiEvents;
                    ok = true;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;

        }

        /// <summary>
        /// Sort the events by ascending absolute time.
        /// Typically, midi events are created ordered with increase time,
        /// but if not, this method must be called before writing to file or playing.
        /// Info: the TmidiEvents list of events will rebuild with this method.
        /// </summary>
        public void MPTK_SortEvents()
        {
            if (TmidiEvents != null && TmidiEvents.Count > 0)
            {
                TmidiEvents = TmidiEvents.OrderBy(o => o.Event.AbsoluteTime).ToList();
            }
            else
                Debug.LogWarning("MidiFileWriter2 - Write - MidiEvents is null or empty");
        }

        /// <summary>
        /// Write Midi file to an OS folder
        /// </summary>
        /// <param name="filename">filename of the midi file</param>
        /// <returns></returns>
        public bool MPTK_WriteToFile(string filename)
        {
            bool ok = false;
            try
            {
                if (TmidiEvents != null && TmidiEvents.Count > 0)
                {
                    MidiFile midiToSave = MPTK_BuildNAudioMidi();
                    MidiFile.Export(filename, midiToSave.Events);
                    ok = true;
                }
                else
                    Debug.LogWarning("MidiFileWriter2 - Write - MidiEvents is null or empty");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>
        /// Build a NAudio midi object from the midi events
        /// </summary>
        /// <returns></returns>
        public MidiFile MPTK_BuildNAudioMidi()
        {
            MidiFile naudioMidi = new MidiFile(MPTK_MidiFileType, MPTK_DeltaTicksPerQuarterNote);
            for (int track = 0; track < trackCount; track++)
            {
                naudioMidi.Events.AddTrack();
                long endLastEvent = 0;
                long prevAbsEvent = 0;
                foreach (TrackMidiEvent tmidi in TmidiEvents)
                {
                    if (tmidi.IndexTrack == track)
                    {
                        tmidi.Event.DeltaTime = (int)(tmidi.Event.AbsoluteTime - prevAbsEvent);
                        prevAbsEvent = tmidi.Event.AbsoluteTime;
                        naudioMidi.Events.AddEvent(tmidi.Event, track);
                        if (endLastEvent < tmidi.Event.AbsoluteTime)
                        {
                            endLastEvent = tmidi.Event.AbsoluteTime;
                            if (tmidi.Event.CommandCode == MidiCommandCode.NoteOn)
                            {
                                // A noteoff event will be created, so time of last event will be more later
                                endLastEvent += tmidi.Event.DeltaTime;
                            }
                        }
                    }
                }
                //Debug.Log($"Close track {track} at {endLastEvent}");
                MetaEvent endTrack = new MetaEvent(MetaEventType.EndTrack, 0, endLastEvent);
                naudioMidi.Events.AddEvent(endTrack, track);
            }

            return naudioMidi;
        }

        /// <summary>
        /// Write Midi file to an OS folder
        /// </summary>
        /// <param name="filename">filename of the midi file</param>
        /// <returns></returns>
        public bool MPTK_Debug(string filename = null)
        {
            bool ok = false;
            try
            {
                if (TmidiEvents != null && TmidiEvents.Count > 0)
                {
                    Debug.Log($"---------------------------------------");
                    Debug.Log($"MPTK_TrackCount:{MPTK_TrackCount}");
                    Debug.Log($"MPTK_DeltaTicksPerQuarterNote:{MPTK_DeltaTicksPerQuarterNote}");

                    foreach (TrackMidiEvent tmidi in TmidiEvents)
                    {
                        string info = $"T:{tmidi.IndexTrack:00} {tmidi.Event.AbsoluteTime:000000} {tmidi.Event.CommandCode,-17} Ch:{tmidi.Event.Channel - 1:00} ";
                        switch (tmidi.Event.CommandCode)
                        {
                            case MidiCommandCode.NoteOn: info += $"{((NoteOnEvent)tmidi.Event).NoteName} ({((NoteOnEvent)tmidi.Event).NoteNumber:00}) L:{((NoteOnEvent)tmidi.Event).NoteLength} V:{((NoteOnEvent)tmidi.Event).Velocity}"; break;
                            case MidiCommandCode.NoteOff: info += $"{((NoteEvent)tmidi.Event).NoteName} ({((NoteEvent)tmidi.Event).NoteNumber:00}) V:{((NoteEvent)tmidi.Event).Velocity}"; break;
                            case MidiCommandCode.PatchChange: info += $"Patch:{((PatchChangeEvent)tmidi.Event).Patch} "; break;
                            case MidiCommandCode.PitchWheelChange: info += $"Pitch:{((PitchWheelChangeEvent)tmidi.Event).Pitch} "; break;
                            case MidiCommandCode.KeyAfterTouch: info += $"Pressure:{((NoteEvent)tmidi.Event).Velocity} "; break;
                            case MidiCommandCode.MetaEvent:
                                MetaEvent meta = (MetaEvent)tmidi.Event;
                                info += meta.MetaEventType + " ";
                                switch (meta.MetaEventType)
                                {
                                    case MetaEventType.SetTempo:
                                        TempoEvent tempo = (TempoEvent)meta;
                                        info += $"MicrosecondsPerQuarterNote:{tempo.MicrosecondsPerQuarterNote}";
                                        //tempo.Tempo
                                        break;

                                    case MetaEventType.TimeSignature:
                                        TimeSignatureEvent timesig = (TimeSignatureEvent)meta;
                                        // Numerator: counts the number of beats in a measure. 
                                        // For example a numerator of 4 means that each bar contains four beats. 

                                        // Denominator: number of quarter notes in a beat.0=ronde, 1=blanche, 2=quarter, 3=eighth, etc. 
                                        // Set default value
                                        info += $"TimeSignature Beats Measure:{timesig.Numerator} Denominator:{timesig.Denominator} Beat Quarter:{System.Convert.ToInt32(Mathf.Pow(2, timesig.Denominator))}";
                                        break;

                                    default:
                                        string text = meta.MetaEventType.ToString() + " " + (meta is TextEvent ? " '" + ((TextEvent)meta).Text + "'" : "");
                                        info += text;
                                        break;

                                }
                                break;

                        }
                        Debug.Log(info);
                    }
                }
                else
                    Debug.LogWarning("MidiFileWriter2 - Write - MidiEvents is null or empty");


            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>
        /// Write Midi file to MidiDB. To be used only in edit mode not in a standalone application.
        /// </summary>
        /// <param name="filename">filename of the midi file without any folder and any extension</param>
        /// <returns></returns>
        public bool MPTK_WriteToMidiDB(string filename)
        {
            bool ok = false;
            try
            {
                if (Application.isEditor)
                {
                    string filenameonly = Path.GetFileNameWithoutExtension(filename) + ".bytes";
                    // Build path to midi folder 
                    string pathMidiFile = Path.Combine(Application.dataPath, MidiPlayerGlobal.PathToMidiFile);
                    string filepath = Path.Combine(pathMidiFile, filenameonly);
                    //Debug.Log(filepath);
                    MPTK_WriteToFile(filepath);
                    //ToolsEditor.CheckMidiSet();
                    //AssetDatabase.Refresh();
                    ok = true;
                }
                else
                    Debug.LogWarning("WriteToMidiDB can be call only in editor mode not in a standalone application");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        private static bool Test(string source, string target)
        {
            bool ok = false;
            try
            {
                MidiFile midifile = new MidiFile(source);
                MidiFile.Export(target, midifile.Events);
                ok = true;
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }
    }
}
