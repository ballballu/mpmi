using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.IO;
using System;
using UnityEngine.Events;
using MPTK.NAudio.Midi;
using System.Linq;
using MidiPlayerTK;
using System.Reflection;

namespace DemoMPTK

{
    /// <summary>
    /// Script for the prefab MidiFilePlayer. 
    /// Play a selected MIDI file. 
    /// List of Midi file must be defined with Midi Player Setup (menu tools).
    /// </summary>
    public class MidiEditController : MonoBehaviour
    {
        float spaceH = 30f;
        float spaceV = 5f;
        public CustomStyle myStyle;
        DateTime startPlaying;
        List<int>[] ChannelIndex = new List<int>[16]; 
        private MidiExternalPlayer temp_player;
        private MidiExternalPlayer real_player;
        private MidiFileWriter2 temp_writer;
        private static Color ButtonColor = new Color(.7f, .9f, .7f, 1f);
        private string[] dir_name;
        private string  fullpath;
        private bool MidiFileLoaded = false;
        private int hold_channel = 0;
        private int hold_event = -1;
        private int hold_pitch = -1;
        private bool pitch_changed = false;
        private int hold_velocity = -1;
        private bool velocity_changed = false;
        private int hold_timepos = 0;
        private bool timepos_changed = false;
        private int hold_length = -1;
        private bool length_changed = false;
        private bool start_Vibrato = false;
        private bool stop_Vibrato = false;
        private bool start_portamento = false;
        private bool stop_portamento = false;
        private int divison = 0;
        private double newtime = 0;
        private bool setNewtime = false;
        private List<TrackMidiEvent> templist = new List<TrackMidiEvent>();

        void Start(){
            if(FindObjectsOfType<MidiExternalPlayer>().Length < 2){
                Debug.Log("Can't find enough midiExternalPlayer1 in the Hierarchy.");
            }
            else{
                temp_player = FindObjectsOfType<MidiExternalPlayer>()[0];
                temp_player.MPTK_DirectSendToPlayer = false;
                real_player = FindObjectsOfType<MidiExternalPlayer>()[1];
                real_player.MPTK_DirectSendToPlayer = true;
            }
            for (int i =0 ;i<16;i++){
                ChannelIndex[i] = new List<int>();
            }
            if (!temp_player.OnEventStartPlayMidi.HasEvent())
            {
                // Set event by script
                Debug.Log("OnEventStartPlayMidi defined by script");
                temp_player.OnEventStartPlayMidi.AddListener(LoadToMfw);
            }
            dir_name = Application.dataPath.Split('/');
        }
        void Update()
        {
            if(setNewtime){
                if(real_player.MPTK_IsPlaying){
                    Debug.Log("timechange");
                    real_player.MPTK_Pause();
                    real_player.MPTK_Position = newtime;
                    real_player.MPTK_UnPause();
                    
                    Debug.Log("newtime" + newtime.ToString());
                    newtime = 0;
                    setNewtime = false;
                }
            }
        }
        void OnGUI()
        {
            if (!HelperDemo.CheckSFExists()) return;

            // Set custom Style. Good for background color 3E619800
            if (myStyle == null) myStyle = new CustomStyle();

            GUILayout.BeginVertical(myStyle.BacgDemos,GUILayout.Width(700));
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent("Quit"), GUILayout.Width(120),GUILayout.Height(40))){
                        Application.Quit();
                    }
                    if (GUILayout.Button("Load midi", GUILayout.Width(120), GUILayout.Height(40)))
                        //Application.OpenURL("file://" + Application.persistentDataPath);
                        LoadAMidi();
                    if (GUILayout.Button("Save midi file", GUILayout.Width(120), GUILayout.Height(40)))
                        //Application.OpenURL("file://" + Application.persistentDataPath);
                        Savemidi();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                    string midiname = "";
                    if(MidiFileLoaded){
                        string[] names = fullpath.Split('/');
                        midiname = names[names.Length-1];
                    }
                    GUILayout.Label("Current midi :" + midiname,GUILayout.Height(40));
                GUILayout.EndHorizontal();                  
                GUILayout.BeginHorizontal();

                    GUILayout.Label("Channel", GUILayout.Width(60),GUILayout.Height(40));
                    int ch_select = (int)GUILayout.HorizontalSlider(hold_channel, 0, 15,  GUILayout.Width(100),GUILayout.Height(40));
                    if(ch_select != hold_channel){
                        hold_channel = ch_select;
                        divison = 0;
                    }
                GUILayout.EndHorizontal();
                //Debug.Log(ChannelIndex[hold_channel].Count);
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal(myStyle.BacgDemos, GUILayout.Width(700));
            GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                if(ChannelIndex[hold_channel].Count !=0){
                    playcontrol();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if(ChannelIndex[hold_channel].Count>=15){
                    GUILayout.Label("Slide to choose range: " + divison.ToString(),GUILayout.Width(200));
                    int div_select = (int)GUILayout.HorizontalSlider(divison, 0, (int)Math.Ceiling((double)ChannelIndex[hold_channel].Count/15),  GUILayout.Width(400));
                    if(divison != div_select){
                        divison = div_select;
                    }                    
                }
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(myStyle.BacgDemos, GUILayout.Width(130));
                    if(ChannelIndex[hold_channel].Count !=0){
                        for(int i =divison*10 ; i < ((divison*10 + 10)< ChannelIndex[hold_channel].Count ? (divison*10 + 10): ChannelIndex[hold_channel].Count); i++){
                            GUILayout.BeginHorizontal();
                            if(temp_writer.TmidiEvents[ChannelIndex[hold_channel][i]].Event.CommandCode == MidiCommandCode.NoteOn){
                                int pitch_ = ((NoteEvent)temp_writer.TmidiEvents[ChannelIndex[hold_channel][i]].Event).NoteNumber;
                                if (GUILayout.Button(new GUIContent("Note "+pitch_.ToString()), GUILayout.Width(120))){
                                    hold_event = ChannelIndex[hold_channel][i];
                                    hold_velocity = -1;
                                    hold_pitch = -1;
                                    hold_timepos = 0;
                                    hold_length = -1;
                                    length_changed = false;
                                    pitch_changed = false;
                                    velocity_changed = false;
                                    timepos_changed = false;
                                    stop_Vibrato = false;
                                    start_Vibrato = false;
                                    start_portamento = false;
                                    stop_portamento = false;
                                }
                            }
                            if(temp_writer.TmidiEvents[ChannelIndex[hold_channel][i]].Event.CommandCode == MidiCommandCode.ControlChange){
                                int controller_value = ((ControlChangeEvent)temp_writer.TmidiEvents[ChannelIndex[hold_channel][i]].Event).ControllerValue;
                                string controller = tostring(((ControlChangeEvent)temp_writer.TmidiEvents[ChannelIndex[hold_channel][i]].Event).Controller);
                                if (GUILayout.Button(new GUIContent(controller + " " + controller_value.ToString()), GUILayout.Width(120))){
                                    hold_event = ChannelIndex[hold_channel][i];
                                    hold_velocity = -1;
                                    hold_pitch = -1;
                                    hold_timepos = 0;
                                    hold_length = -1;
                                    length_changed = false;
                                    pitch_changed = false;
                                    velocity_changed = false;
                                    timepos_changed = false;
                                    stop_Vibrato = false;
                                    start_Vibrato = false;
                                    start_portamento = false;
                                    stop_portamento = false;
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(myStyle.BacgDemos, GUILayout.Height(400), GUILayout.Width(570));
                    if(hold_event != -1){
                        GUILayout.BeginHorizontal();
                            int time_pos = (int)temp_writer.MPTK_ConvertTickToMilli(temp_writer.TmidiEvents[hold_event].Event.AbsoluteTime);
                            GUILayout.Label("Current MIDI Event time: " + toTime(time_pos));
                        GUILayout.EndHorizontal();
                    
                        if(temp_writer.TmidiEvents[hold_event].Event.CommandCode == MidiCommandCode.NoteOn){
                            ShowNoteOnEdit();
                        }
                        else if(temp_writer.TmidiEvents[hold_event].Event.CommandCode == MidiCommandCode.ControlChange)
                        {
                            ShowControlledit();
                        }
                        // else if(temp_writer.TmidiEvents[hold_event].Event.CommandCode == MidiCommandCode.NoteOff){
                        //     ShowNoteOffEdit();
                        // }  
                        GUILayout.BeginHorizontal();
                        if(temp_writer.TmidiEvents[hold_event].Event.CommandCode != MidiCommandCode.ControlChange){
                            if (GUILayout.Button("Vibrato on", GUILayout.Width(100), GUILayout.Height(30))){
                                start_Vibrato = true;
                            }
                            if (GUILayout.Button("Vibrato off", GUILayout.Width(100), GUILayout.Height(30))){
                                stop_Vibrato = true;
                            }
                            if (GUILayout.Button("Portamento on", GUILayout.Width(110), GUILayout.Height(30))){
                                start_portamento = true;
                            }
                            if (GUILayout.Button("Portamento off", GUILayout.Width(120), GUILayout.Height(30))){
                                stop_portamento = true;
                            }
                            if (GUILayout.Button("Save change", GUILayout.Width(110), GUILayout.Height(30))){
                                SaveChange();
                            }      
                        }                       
                        GUILayout.EndHorizontal();
                    }
                GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
        private void LoadAMidi(){
            temp_player.MPTK_Stop();
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.filter = "Midi File(*.mid)\0*.mid";
            ofn.file = new string(new char[512]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[128]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            string path = Application.streamingAssetsPath;
            path = path.Replace('/', '\\');
            ofn.initialDir = path;
            ofn.title = "Open Midi File";
            ofn.defExt = "MID";
            ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
            if (WindowDll.GetOpenFileName(ofn)){
                fullpath = "file://";
                ofn.file = ofn.file.Replace(@"\", @"/");
                fullpath += ofn.file;
                Debug.Log("Selected file with full path: " + fullpath);
                temp_player.MPTK_MidiName = fullpath;
                temp_player.MPTK_Play();
                //LoadToMfw();
            }            
        }
        
        public void LoadToMfw(string name){
            Debug.Log("LoadToMfw");
            temp_writer = new MidiFileWriter2(temp_player.MPTK_DeltaTicksPerQuarterNote, 1);
            temp_writer.MPTK_LoadFromMPTK(temp_player.MPTK_MidiEvents, temp_player.MPTK_TrackCount);
            temp_player.MPTK_Stop();
            temp_writer.MPTK_SortEvents();
            UpdateChannelList();
            // for(int i =0 ;i<16;i++){
            //     ChannelIndex[i] = new List<int>();
            // }
            // //int i=0;
            // // foreach(TrackMidiEvent trackevnet in temp_writer.TmidiEvents){
            // //     if(trackevnet.Event.CommandCode == MidiCommandCode.NoteOn || trackevnet.Event.CommandCode == MidiCommandCode.NoteOff ||
            // //         trackevnet.Event.CommandCode == MidiCommandCode.ControlChange ){
            // //             Debug.Log("have evnet");
            // //             ChannelIndex[trackevnet.Event.Channel-1].Add(i);
            // //         }
            // //     i+=1;
            // // }
            // for(int i = 0;i<temp_writer.TmidiEvents.Count;i++){
            //     if(temp_writer.TmidiEvents[i].Event.CommandCode == MidiCommandCode.NoteOn || temp_writer.TmidiEvents[i].Event.CommandCode == MidiCommandCode.NoteOff ||
            //         temp_writer.TmidiEvents[i].Event.CommandCode == MidiCommandCode.ControlChange ){
            //             Debug.Log("have evnet");
            //             ChannelIndex[temp_writer.TmidiEvents[i].Event.Channel-1].Add(i);
            //     }
            // }
            MidiFileLoaded = true;
            divison = 0;
            hold_channel = 0;
            hold_event = -1;
            hold_pitch = -1;
            hold_velocity = -1;
            hold_timepos = 0;
            hold_length = -1;
            length_changed = false;
            pitch_changed = false;
            velocity_changed = false;
            timepos_changed = false;
            stop_Vibrato = false;
            start_Vibrato = false;
            start_portamento = false;
            stop_portamento = false;
        }
        public string tostring(MidiCommandCode CommandCode){
            if(CommandCode == MidiCommandCode.NoteOff){
                return "noteOff";
            }
            else if(CommandCode == MidiCommandCode.NoteOn){
                return "noteOn";
            }
            else if(CommandCode == MidiCommandCode.ControlChange){
                return "ControlChange";
            }
            else
                return "other";
        }
        public string tostring(MidiController controller){
            if(controller == MidiController.Portamento){
                return "Portamento";
            }
            else if(controller == MidiController.Modulation){
                return "Vibrato";
            }
            else{
                return "CC";
            }

            
        }
        public string toTime(int t){
            int hour = t/(1000*60*24);
            int min = (t - hour*(1000*60*24))/(1000*60);
            int sec = (t - hour*(1000*60*24) - min*(1000*60))/1000;
            int msec = t - hour*(1000*60*24) - min*(1000*60) - sec*1000;
            //string timeString = hour.ToString() +":"+ min.ToString() +":"+ sec.ToString() +":"+ msec.ToString();
            string timeString = string.Format("{0:00}:{1:00}:{2:00}:{3:000}", hour, min, sec, msec);
            return timeString;
        }

        public void ShowNoteOnEdit(){
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current MIDI type: " + temp_writer.TmidiEvents[hold_event].Event.CommandCode.ToString());
            
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if(hold_timepos == -1){
                GUILayout.Label("Move Time position " + hold_timepos.ToString() ,GUILayout.Width(200));
                int pos_select = (int)GUILayout.HorizontalSlider(hold_timepos, -8, 8,  GUILayout.Width(100));
                hold_timepos = pos_select;
            }
            else{
                GUILayout.Label("Move Time position " + hold_timepos.ToString() + " ( 1/8 Note) " ,GUILayout.Width(200));
                int pos_select = (int)GUILayout.HorizontalSlider(hold_timepos, -32, 32,  GUILayout.Width(100));
                if(hold_timepos != pos_select){
                    hold_timepos = pos_select;
                    timepos_changed = true;
                }
                hold_timepos = pos_select;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if(hold_length == -1){
                int lengthTo32thNote = (int)(8*temp_writer.MPTK_ConvertTickToMilli(((NoteOnEvent)temp_writer.TmidiEvents[hold_event].Event).NoteLength)*1000/temp_writer.MPTK_MicrosecondsPerQuaterNote);
                Debug.Log(" NoteLength "+ ((NoteOnEvent)temp_writer.TmidiEvents[hold_event].Event).NoteLength.ToString());
                GUILayout.Label("NoteLength " + lengthTo32thNote.ToString()+" ( 1/32 Note) ",GUILayout.Width(200));
                int length_select = (int)GUILayout.HorizontalSlider(lengthTo32thNote, 1, 128,  GUILayout.Width(100));
                hold_length = length_select;
            }
            else{
                // int length2QuaterNote = (int)(4*temp_writer.MPTK_ConvertTickToMilli(((NoteOnEvent)temp_writer.TmidiEvents[hold_event].Event).NoteLength)*1000/temp_writer.MPTK_MicrosecondsPerQuaterNote);
                GUILayout.Label("NoteLength " + hold_length.ToString()+" ( 1/32 Note) ",GUILayout.Width(200));
                int length_select = (int)GUILayout.HorizontalSlider(hold_length, 1, 128,  GUILayout.Width(100));
                if(hold_length != length_select){
                    hold_length = length_select;
                    length_changed = true;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pitch",GUILayout.Width(70));
            if(hold_pitch == -1){
                GUILayout.Label(((NoteOnEvent)temp_writer.TmidiEvents[hold_event].Event).NoteNumber.ToString(),GUILayout.Width(70));
                int picth_select = (int)GUILayout.HorizontalSlider(((NoteOnEvent)temp_writer.TmidiEvents[hold_event].Event).NoteNumber, 0, 127,  GUILayout.Width(100));
                hold_pitch = picth_select;
            }
            else{
                GUILayout.Label(hold_pitch.ToString(),GUILayout.Width(70));
                int picth_select = (int)GUILayout.HorizontalSlider(hold_pitch, 0, 127,  GUILayout.Width(100));
                if(hold_pitch != picth_select){
                    hold_pitch = picth_select;
                    pitch_changed = true;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Velocity",GUILayout.Width(70));
            if(hold_velocity == -1){
                GUILayout.Label(((NoteOnEvent)temp_writer.TmidiEvents[hold_event].Event).Velocity.ToString(),GUILayout.Width(70));
                int vel_select = (int)GUILayout.HorizontalSlider(((NoteOnEvent)temp_writer.TmidiEvents[hold_event].Event).Velocity, 0, 127,  GUILayout.Width(100));
                hold_velocity = vel_select;
            }
            else{
                GUILayout.Label(hold_velocity.ToString(),GUILayout.Width(70));
                int vel_select = (int)GUILayout.HorizontalSlider(hold_velocity, 0, 127,  GUILayout.Width(100));
                if(hold_velocity != vel_select){
                    hold_velocity = vel_select;
                    velocity_changed = true;
                }
            }
            GUILayout.EndHorizontal();            
        }

        public void ShowNoteOffEdit(){
            // GUILayout.BeginHorizontal();
            // GUILayout.Label(((NoteEvent)temp_writer.TmidiEvents[hold_event].Event).NoteLength.ToString(),GUILayout.Width(70));
            // GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current MIDI type: " + temp_writer.TmidiEvents[hold_event].Event.CommandCode.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pitch",GUILayout.Width(70));
            if(hold_pitch == -1){
                GUILayout.Label(((NoteEvent)temp_writer.TmidiEvents[hold_event].Event).NoteNumber.ToString(),GUILayout.Width(70));
                int picth_select = (int)GUILayout.HorizontalSlider(((NoteEvent)temp_writer.TmidiEvents[hold_event].Event).NoteNumber, 0, 127,  GUILayout.Width(100));
                hold_pitch = picth_select;
            }
            else{
                GUILayout.Label(hold_pitch.ToString(),GUILayout.Width(70));
                int picth_select = (int)GUILayout.HorizontalSlider(hold_pitch, 0, 127,  GUILayout.Width(100));
                hold_pitch = picth_select;                                  
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Velocity",GUILayout.Width(70));
            if(hold_velocity == -1){
                GUILayout.Label(((NoteEvent)temp_writer.TmidiEvents[hold_event].Event).Velocity.ToString(),GUILayout.Width(70));
                int vel_select = (int)GUILayout.HorizontalSlider(((NoteEvent)temp_writer.TmidiEvents[hold_event].Event).Velocity, 0, 127,  GUILayout.Width(100));
                hold_velocity = vel_select;
            }
            else{
                GUILayout.Label(hold_velocity.ToString(),GUILayout.Width(70));
                int vel_select = (int)GUILayout.HorizontalSlider(hold_velocity, 0, 127,  GUILayout.Width(100));
                hold_velocity = vel_select;                                  
            }
            GUILayout.EndHorizontal();            
        }

        public void ShowControlledit(){
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current MIDI type: " + temp_writer.TmidiEvents[hold_event].Event.CommandCode.ToString());
            GUILayout.EndHorizontal();      

            GUILayout.BeginHorizontal();
            GUILayout.Label("Controller type: " + tostring(((ControlChangeEvent)temp_writer.TmidiEvents[hold_event].Event).Controller));
            GUILayout.EndHorizontal();    

            GUILayout.BeginHorizontal();
            GUILayout.Label("Controller Value: " + ((ControlChangeEvent)temp_writer.TmidiEvents[hold_event].Event).ControllerValue.ToString());
            GUILayout.EndHorizontal();     
        }

        public void SaveChange(){
            // if(temp_writer.TmidiEvents[hold_event].Event.CommandCode == MidiCommandCode.NoteOn){
            //     //temp_writer.TmidiEvents[hold_event].Event.AbsoluteTime = hold_timepos;
            //     ((NoteEvent)temp_writer.TmidiEvents[hold_event].Event).NoteNumber = hold_pitch;
            //     ((NoteEvent)temp_writer.TmidiEvents[hold_event].Event).Velocity = hold_velocity;
            // }
            int offeventIndex = findoffevent(hold_event);
            Debug.Log(" on event " + hold_event.ToString()+ " off event " + offeventIndex.ToString());
            if(pitch_changed){
                if(temp_writer.TmidiEvents[hold_event].Event.CommandCode == MidiCommandCode.NoteOn){
                    ((NoteEvent)temp_writer.TmidiEvents[hold_event].Event).NoteNumber = hold_pitch;
                }
                if(offeventIndex != -1){
                    ((NoteEvent)temp_writer.TmidiEvents[offeventIndex].Event).NoteNumber = hold_pitch;
                }
            }
            if(velocity_changed){
                if(temp_writer.TmidiEvents[hold_event].Event.CommandCode == MidiCommandCode.NoteOn){
                    ((NoteEvent)temp_writer.TmidiEvents[hold_event].Event).Velocity = hold_velocity;
                }
                if(offeventIndex != -1){
                    ((NoteEvent)temp_writer.TmidiEvents[offeventIndex].Event).Velocity = hold_velocity;
                }
            }
            int temp_length = ((NoteOnEvent)temp_writer.TmidiEvents[hold_event].Event).NoteLength;
            int pitch = ((NoteOnEvent)temp_writer.TmidiEvents[hold_event].Event).NoteNumber;
            Debug.Log(" temp_length "+ temp_length.ToString());
            Debug.Log(" pitch "+ pitch.ToString());
            if(timepos_changed){
                float length8thnote = temp_writer.MPTK_MicrosecondsPerQuaterNote/(1000*2);
                Debug.Log("length8thnote "+ length8thnote.ToString());
                long changed_pos = temp_writer.TmidiEvents[hold_event].Event.AbsoluteTime + temp_writer.MPTK_ConvertMilliToTick(hold_timepos * length8thnote);
                temp_writer.TmidiEvents[hold_event].Event.AbsoluteTime = changed_pos < 0 ? 0:changed_pos;
                temp_writer.TmidiEvents[offeventIndex].Event.AbsoluteTime = temp_writer.TmidiEvents[hold_event].Event.AbsoluteTime + temp_length;
            }
            if(length_changed){
                float length32thnote = (float)(temp_writer.MPTK_MicrosecondsPerQuaterNote/(1000*8.0));
                Debug.Log("length32thnote "+ length32thnote.ToString());
                long change_length = temp_writer.TmidiEvents[hold_event].Event.AbsoluteTime + temp_writer.MPTK_ConvertMilliToTick(hold_length * length32thnote);
                temp_writer.TmidiEvents[offeventIndex].Event.AbsoluteTime = change_length;
            }
            int track = temp_writer.TmidiEvents[hold_event].IndexTrack;
            long absoluteTime_on =  temp_writer.TmidiEvents[hold_event].Event.AbsoluteTime;
            long absoluteTime_off = temp_writer.TmidiEvents[offeventIndex].Event.AbsoluteTime;
            int channel = temp_writer.TmidiEvents[hold_event].Event.Channel -1;
            if(stop_Vibrato){
                temp_writer.MPTK_AddControlChange(track, absoluteTime_off+1, channel, MPTKController.Modulation, 0);
            }
            if(start_Vibrato){
                temp_writer.MPTK_AddControlChange(track, absoluteTime_on-1<0?0:absoluteTime_on-1, channel, MPTKController.Modulation, 127);
            }
            if(stop_portamento){
                temp_writer.MPTK_AddControlChange(track, absoluteTime_off+1, channel, MPTKController.Portamento, 0);
            }
            if(start_portamento){
                temp_writer.MPTK_AddControlChange(track, absoluteTime_on-2<0?0:absoluteTime_on-1, channel, MPTKController.Portamento, 127);
                temp_writer.MPTK_AddControlChange(track, absoluteTime_on-2<0?0:absoluteTime_on-1, channel, MPTKController.PORTAMENTO_TIME_MSB, 127);
            }
            hold_event = -1;
            hold_velocity = -1;
            hold_pitch = -1;
            hold_timepos = 0;
            hold_length = -1;
            length_changed = false;
            pitch_changed = false;
            velocity_changed = false;
            timepos_changed = false;
            stop_Vibrato = false;
            start_Vibrato = false;
            start_portamento = false;
            stop_portamento = false;
            temp_writer.MPTK_SortEvents();
            UpdateChannelList();
        }

        public int findoffevent(int NoteOnIndex){
            int index = NoteOnIndex;
            int track = temp_writer.TmidiEvents[NoteOnIndex].IndexTrack;
            int channel = temp_writer.TmidiEvents[NoteOnIndex].Event.Channel;
            int note = ((NoteEvent)temp_writer.TmidiEvents[NoteOnIndex].Event).NoteNumber;
            while (NoteOnIndex != temp_writer.TmidiEvents.Count)
            {
                if (temp_writer.TmidiEvents[index].IndexTrack == track &&
                    temp_writer.TmidiEvents[index].Event.CommandCode == MidiCommandCode.NoteOff &&
                    temp_writer.TmidiEvents[index].Event.Channel == channel &&
                    ((NoteEvent)temp_writer.TmidiEvents[index].Event).NoteNumber == note)
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        public void UpdateChannelList(){
            for(int i =0 ;i<16;i++){
                ChannelIndex[i] = new List<int>();
            }
            //int i=0;
            // foreach(TrackMidiEvent trackevnet in temp_writer.TmidiEvents){
            //     if(trackevnet.Event.CommandCode == MidiCommandCode.NoteOn || trackevnet.Event.CommandCode == MidiCommandCode.NoteOff ||
            //         trackevnet.Event.CommandCode == MidiCommandCode.ControlChange ){
            //             Debug.Log("have evnet");
            //             ChannelIndex[trackevnet.Event.Channel-1].Add(i);
            //         }
            //     i+=1;
            // }
            for(int i = 0;i<temp_writer.TmidiEvents.Count;i++){
                if(temp_writer.TmidiEvents[i].Event.CommandCode == MidiCommandCode.NoteOn ||
                    temp_writer.TmidiEvents[i].Event.CommandCode == MidiCommandCode.ControlChange ){
                        Debug.Log("have evnet");
                        ChannelIndex[temp_writer.TmidiEvents[i].Event.Channel-1].Add(i);
                }
            }
        }

        public void playcontrol(){
            if (real_player.MPTK_IsPlaying && !real_player.MPTK_IsPaused){
                GUI.color = ButtonColor;
            }
            if (GUILayout.Button(new GUIContent("Play", ""), GUILayout.Width(100))){
                    DircetPlay();
                    real_player.MPTK_Stop();
                    real_player.MPTK_Play();
            }
            GUI.color = Color.white;
            if (GUILayout.Button(new GUIContent("Stop", ""), GUILayout.Width(100))){
                real_player.MPTK_Stop();
            }
        }
        
        public void DircetPlay(){
            string filename = Path.Combine(Application.persistentDataPath, "tempmidi" + ".mid");
            int currInedx = (ChannelIndex[hold_channel][divison*10]-3)<0?0:ChannelIndex[hold_channel][divison*10]-3;
            double newtime_ = Math.Round(temp_writer.MPTK_ConvertTickToMilli(temp_writer.TmidiEvents[currInedx].Event.AbsoluteTime));
            newtime = newtime_;
            Debug.Log("currInedx " + currInedx.ToString());
            
            // Sort the events by ascending absolute time (optional)
            temp_writer.MPTK_SortEvents();
            //temp_writer.MPTK_Debug();
            // Write the MIDI file
            ProcessPortamento();
            temp_writer.MPTK_SortEvents();
            temp_writer.MPTK_WriteToFile(filename);
            Debug.Log("Write Midi file:" + filename);

            real_player.MPTK_Stop();
            real_player.MPTK_MidiName = "file://" + filename;
            real_player.MPTK_Play();
            temp_writer.TmidiEvents = new List<TrackMidiEvent>();
            temp_writer.TmidiEvents = copylist(templist);
            setNewtime = true;
        }
        public void ProcessPortamento(){
            long length32note = temp_writer.MPTK_ConvertMilliToTick(temp_writer.MPTK_MicrosecondsPerQuaterNote/8000);
            long length16note = temp_writer.MPTK_ConvertMilliToTick(temp_writer.MPTK_MicrosecondsPerQuaterNote/4000);
            long length8note = temp_writer.MPTK_ConvertMilliToTick(temp_writer.MPTK_MicrosecondsPerQuaterNote/2000);
            long length4note = temp_writer.MPTK_ConvertMilliToTick(temp_writer.MPTK_MicrosecondsPerQuaterNote/1000);
            long length2note = temp_writer.MPTK_ConvertMilliToTick(temp_writer.MPTK_MicrosecondsPerQuaterNote/500);
            templist = new List<TrackMidiEvent>();
            templist = copylist(temp_writer.TmidiEvents);
            int totalevent = temp_writer.TmidiEvents.Count;
            List<int> Portamentochannel = new List<int>();
            for(int i = 0; i<16;i++){
                if(ChannelIndex[i].Count!=0){
                    for(int j =0; j<ChannelIndex[i].Count;j++){
                        if(temp_writer.TmidiEvents[ChannelIndex[i][j]].Event.CommandCode==MidiCommandCode.ControlChange &&
                            ((ControlChangeEvent)temp_writer.TmidiEvents[ChannelIndex[i][j]].Event).Controller==MidiController.Portamento &&
                            ((ControlChangeEvent)temp_writer.TmidiEvents[ChannelIndex[i][j]].Event).ControllerValue >= 64){
                                Portamentochannel.Add(i);
                                Debug.Log("find Portamentochannel " + i.ToString());
                                break;
                            }
                    }
                }
            }
            //setting generator param for channel where Portamento happen
            for(int i =0; i<Portamentochannel.Count;i++){
                temp_writer.MPTK_AddControlChange(5,0,Portamentochannel[i],MPTKController.RPN_MSB,0);
                temp_writer.MPTK_AddControlChange(5,0,Portamentochannel[i],MPTKController.RPN_LSB,(int)midi_rpn_event.RPN_PITCH_BEND_RANGE);
                temp_writer.MPTK_AddControlChange(5,0,Portamentochannel[i],MPTKController.DATA_ENTRY_MSB,24);
                temp_writer.MPTK_AddControlChange(5,0,Portamentochannel[i],MPTKController.DATA_ENTRY_LSB,0);
            }
            for(int i= 0; i < Portamentochannel.Count;i++){
                int curr_ch = Portamentochannel[i];
                int start = 0;
                int end = ChannelIndex[curr_ch].Count-1;
                int portamentoOn_pos = findportamentoOn(curr_ch,start,end);
                int portamentoOff_pos = findportamentoOff(curr_ch,portamentoOn_pos,end);
                while(portamentoOn_pos<end){
                    int On_pos = portamentoOn_pos;
                    while(On_pos<portamentoOff_pos){
                        if(temp_writer.TmidiEvents[ChannelIndex[curr_ch][On_pos]].Event.CommandCode == MidiCommandCode.NoteOn){
                            int nextNote_pos = findnextnote(curr_ch,On_pos+1,portamentoOff_pos);
                            Debug.Log("nextNote_pos "+ nextNote_pos.ToString() );
                            Debug.Log("nextnote " + temp_writer.TmidiEvents[ChannelIndex[curr_ch][nextNote_pos]].Event.CommandCode.ToString());
                            Debug.Log("portamentoOff_pos " + portamentoOff_pos.ToString());
                            if(nextNote_pos <portamentoOff_pos){
                                long time_dur_on_on = temp_writer.TmidiEvents[ChannelIndex[curr_ch][nextNote_pos]].Event.AbsoluteTime - temp_writer.TmidiEvents[ChannelIndex[curr_ch][On_pos]].Event.AbsoluteTime;
                                int On_off  = findoffevent(ChannelIndex[curr_ch][On_pos]);
                                int nextNote_off = findoffevent(ChannelIndex[curr_ch][nextNote_pos]);
                                long time_dur_off_on =  temp_writer.TmidiEvents[ChannelIndex[curr_ch][nextNote_pos]].Event.AbsoluteTime - temp_writer.TmidiEvents[On_off].Event.AbsoluteTime;
                                if(time_dur_off_on < 0 // only work on multi note/*((NoteOnEvent)temp_writer.TmidiEvents[ChannelIndex[curr_ch][On_pos]].Event).NoteLength *0.4 */
                                    && time_dur_on_on > length32note
                                    && ((NoteOnEvent)temp_writer.TmidiEvents[ChannelIndex[curr_ch][On_pos]].Event).NoteLength >= length32note
                                    && ((NoteOnEvent)temp_writer.TmidiEvents[ChannelIndex[curr_ch][nextNote_pos]].Event).NoteLength>= length32note){
                                    temp_writer.TmidiEvents[On_off].Event.AbsoluteTime = temp_writer.TmidiEvents[ChannelIndex[curr_ch][nextNote_pos]].Event.AbsoluteTime-1;
                                    long insert_pitch_start = temp_writer.TmidiEvents[ChannelIndex[curr_ch][On_pos]].Event.AbsoluteTime + (int)(0.7*(temp_writer.TmidiEvents[On_off].Event.AbsoluteTime - temp_writer.TmidiEvents[ChannelIndex[curr_ch][On_pos]].Event.AbsoluteTime));
                                    long insert_pitch_mid = temp_writer.TmidiEvents[On_off].Event.AbsoluteTime;
                                    long insert_pitch_end = temp_writer.TmidiEvents[ChannelIndex[curr_ch][nextNote_pos]].Event.AbsoluteTime + (int)(0.3*((NoteOnEvent)temp_writer.TmidiEvents[ChannelIndex[curr_ch][nextNote_pos]].Event).NoteLength);
                                    int fromkey = ((NoteOnEvent)temp_writer.TmidiEvents[ChannelIndex[curr_ch][On_pos]].Event).NoteNumber;
                                    int tokey = ((NoteOnEvent)temp_writer.TmidiEvents[ChannelIndex[curr_ch][nextNote_pos]].Event).NoteNumber;
                                    int track = temp_writer.TmidiEvents[On_off].IndexTrack;
                                    insert_pitch(curr_ch,track, insert_pitch_start, insert_pitch_mid, insert_pitch_end, fromkey, tokey);
                                }
                            }
                            On_pos = nextNote_pos;
                        }
                        else{
                            On_pos++;
                        }
                    }
                    portamentoOn_pos = findportamentoOn(curr_ch,portamentoOff_pos,end);
                    portamentoOff_pos = findportamentoOff(curr_ch,portamentoOn_pos,end);
                }
            }

        }
        public List<TrackMidiEvent> copylist(List<TrackMidiEvent> obj)
        {
            List<TrackMidiEvent> copy = new List<TrackMidiEvent>();
            foreach(TrackMidiEvent e in obj){
                copy.Add(new TrackMidiEvent{IndexEvent = e.IndexEvent, IndexTrack = e.IndexTrack,AbsoluteQuantize = e.AbsoluteQuantize,RealTime=e.RealTime,Event=e.Event.Clone()});
            }
            return copy;
        }

        public int findportamentoOn(int ch, int start, int end){
            if(start==end) return end;
            for(int i = start; i<=end;i++){
                if(temp_writer.TmidiEvents[ChannelIndex[ch][i]].Event.CommandCode == MidiCommandCode.ControlChange && 
                    ((ControlChangeEvent)temp_writer.TmidiEvents[ChannelIndex[ch][i]].Event).Controller == MidiController.Portamento &&
                    ((ControlChangeEvent)temp_writer.TmidiEvents[ChannelIndex[ch][i]].Event).ControllerValue >= 64){
                        return i;
                }
            }
            return end;
        }
        public int findportamentoOff(int ch, int start, int end){
            if(start==end) return end;
            for(int i = start; i<=end;i++){
                if(temp_writer.TmidiEvents[ChannelIndex[ch][i]].Event.CommandCode == MidiCommandCode.ControlChange && 
                    ((ControlChangeEvent)temp_writer.TmidiEvents[ChannelIndex[ch][i]].Event).Controller == MidiController.Portamento &&
                    ((ControlChangeEvent)temp_writer.TmidiEvents[ChannelIndex[ch][i]].Event).ControllerValue < 64){
                        return i;
                }
            }
            return end;            
        }
        public int findnextnote(int ch, int start, int end){
            if(start==end) return end;
            for(int i = start; i<=end;i++){
                if(temp_writer.TmidiEvents[ChannelIndex[ch][i]].Event.CommandCode == MidiCommandCode.NoteOn){
                    Debug.Log("in find " + i.ToString());
                    return i;
                }
            }
            return end;            
        }
        public void insert_pitch(int curr_ch, int track, long insert_pitch_start, long insert_pitch_mid, long insert_pitch_end, int fromkey, int tokey){
            float step = (float)((fromkey-tokey)/(2.0*24*60));
            Debug.Log("insert step " + step.ToString());
            long time_step = (insert_pitch_mid - insert_pitch_start)/30;
            long insert_time = insert_pitch_start;
            for(int i = 1; i<=30; i++){
                insert_time += time_step;
                temp_writer.MPTK_AddPitchWheelChange(track, insert_time-1, curr_ch, (float)(0.5+step*i));
                Debug.Log(String.Format("{0:N8}", (float)(0.5+step*i)));
            }
            temp_writer.MPTK_AddPitchWheelChange(track, insert_pitch_mid, curr_ch, 0.5f); 
            insert_time = insert_pitch_mid+1;
            time_step = (insert_pitch_end - insert_time)/30;
            for(int i = 29; i>=0; i--){
                temp_writer.MPTK_AddPitchWheelChange(track, insert_time, curr_ch, (float)(0.5-step*i));
                insert_time += time_step;
            }
            temp_writer.MPTK_AddPitchWheelChange(track, insert_time, curr_ch, 0.5f);            
        }
        public void Savemidi(){
            if(MidiFileLoaded){
                string filepath = "output" + ".mid";
                filepath = Path.Combine(Application.dataPath.Replace(dir_name[dir_name.Length-1],"Output_midi"), filepath);
                if(!Directory.Exists(Application.dataPath.Replace(dir_name[dir_name.Length-1],"Output_midi"))){
                    try{
                        Directory.CreateDirectory(Application.dataPath.Replace(dir_name[dir_name.Length-1],"Output_midi"));
                    }catch(Exception e){

                    }
                }
                // Sort the events by ascending absolute time (optional)
                temp_writer.MPTK_SortEvents();
                //temp_writer.MPTK_Debug();
                // Write the MIDI file
                temp_writer.MPTK_WriteToFile(filepath);
                Debug.Log("Write Midi file:" + filepath);
            }
        }
    }
}

