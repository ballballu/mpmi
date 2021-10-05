using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace MidiPlayerTK
{

    public partial class MPTKEvent : ICloneable
    {

        /// <summary>
        /// List of generators modifier associated to the voices being played. Null if any modifier defined.
        /// </summary>
        public GenModifier[] GensModifier;

        /// <summary>
        /// [Pro] Apply modification on default SoundFont generator value. Can be applied independently for each notes,\n
        /// can be applied before the note is played and in <b>real time</b> when the note is playing.\n
        /// Each generator has a specific range of value, <a href=" https://paxstellar.fr/wp-content/uploads/2021/01/GeneratorModulation.png"><b>see generators list here.</b></a>\n
        /// Also the value used by this method is normalized (value between 0 and 1).
        /// <a href="https://paxstellar.fr/class-mptkevent#Generator-List"><b>See here  for more details.</b></a>\n
        /// @code
        /// // Create a midi event for a C5 note (60) with default value: infinite duration, channel = 0, and velocity = 127 (max)
        /// mptkEvent = new MPTKEvent() { Value = 60 };
        /// 
        /// // Fine tuning (pitch)
        /// mptkEvent.MTPK_ModifySynthParameter(fluid_gen_type.GEN_FINETUNE, 0.52f, MPTKModeGeneratorChange.Override);
        /// 
        /// // Change low pass filter frequency
        /// mptkEvent.MTPK_ModifySynthParameter(fluid_gen_type.GEN_FILTERFC, 0.6f, MPTKModeGeneratorChange.Override);
        /// 
        /// midiStream.MPTK_PlayDirectEvent(mptkEvent);
        /// @endcode
        /// </summary>
        /// <param name="genType">Type of generator to modify. Not all generators are authorized to real time modification.\n
        ///! @li @c  GEN_MODLFOTOPITCH	Modulation LFO to pitch
        ///! @li @c  GEN_VIBLFOTOPITCH	Vibrato LFO to pitch
        ///! @li @c  GEN_MODENVTOPITCH	Modulation envelope to pitch
        ///! @li @c  GEN_FILTERFC	Filter cutoff
        ///! @li @c  GEN_FILTERQ	Filter Q
        ///! @li @c  GEN_MODLFOTOFILTERFC	Modulation LFO to filter cutoff
        ///! @li @c  GEN_MODENVTOFILTERFC	Modulation envelope to filter cutoff
        ///! @li @c  GEN_MODLFOTOVOL	Modulation LFO to volume
        ///! @li @c  GEN_CHORUSSEND	Chorus send amount
        ///! @li @c  GEN_REVERBSEND	Reverb send amount
        ///! @li @c  GEN_PAN	Stereo panning
        ///! @li @c  GEN_MODLFODELAY	Modulation LFO delay
        ///! @li @c  GEN_MODLFOFREQ	Modulation LFO frequency
        ///! @li @c  GEN_VIBLFODELAY	Vibrato LFO delay
        ///! @li @c  GEN_VIBLFOFREQ	Vibrato LFO frequency
        ///! @li @c  GEN_MODENVDELAY	Modulation envelope delay
        ///! @li @c  GEN_MODENVATTACK	Modulation envelope attack
        ///! @li @c  GEN_MODENVHOLD	Modulation envelope hold
        ///! @li @c  GEN_MODENVDECAY	Modulation envelope decay
        ///! @li @c  GEN_MODENVSUSTAIN	Modulation envelope sustain
        ///! @li @c  GEN_MODENVRELEASE	Modulation envelope release
        ///! @li @c  GEN_VOLENVDELAY	Volume envelope delay
        ///! @li @c  GEN_VOLENVATTACK	Volume envelope attack
        ///! @li @c  GEN_VOLENVHOLD	Volume envelope hold
        ///! @li @c  GEN_VOLENVDECAY	Volume envelope decay
        ///! @li @c  GEN_VOLENVSUSTAIN	Volume envelope sustain
        ///! @li @c  GEN_VOLENVRELEASE	Volume envelope release
        ///! @li @c  GEN_ATTENUATION	Volume attenuation
        ///! @li @c  GEN_COARSETUNE	Coarse tuning
        ///! @li @c  GEN_FINETUNE	Fine tuning
        /// </param>
        /// <param name="value">Normalized value for the generator between 0 and 1.\n
        ///  <a href="https://paxstellar.fr/class-mptkevent#Generator-List"><b>See here real value for each parameters.</b></a>\n
        ///! @li @c  0 set the minimum value for the generator. For example, with an envelope parameter, 0 will set -12000 (min for this type of parameters.
        ///! @li @c  1 set the maximum value for the generator. For example, with an envelope parameter, 1 will set 12000 (max for this type of parameters.
        /// </param>
        /// <param name="mode">Define how to apply the value\n
        ///! @li @c  Override: the SoundFont value is overridden.
        ///! @li @c  Reinforce: the value is added to the default value.
        ///! @li @c  Restaure: the default SoundFont value is used.
        /// </param>
        /// <returns>true if change has been done</returns>
        public bool MTPK_ModifySynthParameter(fluid_gen_type genType, float value, MPTKModeGeneratorChange mode)
        {
            bool result = false;
            int genId = ConvertIdToIndex(genType);
            if (genId >= 0)
            {
                try
                {
                    // If a list of modifier is already associated to this event ?
                    if (GensModifier == null)
                    {
                        GenModifier.InitListGenerator();
                        GensModifier = new GenModifier[Enum.GetNames(typeof(fluid_gen_type)).Length];
                    }
                    if (GensModifier[genId] == null) GensModifier[genId] = new GenModifier();
                    GensModifier[genId].Mode = mode;
                    GensModifier[genId].NormalizedVal = value;
                    GensModifier[genId].SoundFontVal = Mathf.Lerp(fluid_gen_info.FluidGenInfo[genId].min, fluid_gen_info.FluidGenInfo[genId].max, value);

                    // If event is already playing (voices are defined) applied change in real time
                    if (Voices != null)
                        foreach (fluid_voice voice in Voices)
                            voice.fluid_voice_update_param(genId);
                    result = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"MTPK_ModifySynthParameter - {ex.Message}");
                }
            }
            return result;
        }

        private static int ConvertIdToIndex(fluid_gen_type genType)
        {
            int genId = (int)genType;
            if (genId < 0 || genId >= Enum.GetNames(typeof(fluid_gen_type)).Length || !fluid_gen_info.FluidGenInfo[genId].RealTimeChange)
            {
                Debug.LogWarning($"ConvertIdToIndex - fluid_gen_type {genType} {genId} - cannot be modified or outside range");
                return -1;
            }
            return genId;
        }

        /// <summary>
        /// [Pro] V2.88.2 Get the default soundfont value for the generator.\n
        /// Each generator has a specific range of value, <a href=" https://paxstellar.fr/wp-content/uploads/2021/01/GeneratorModulation.png"><b>see generators list here.</b></a>\n
        /// Also the value returned by this method is normalized (value between 0 and 1).
        /// </summary>
        /// <param name="genType">see #MTPK_ModifySynthParameter</param>
        /// <returns>Return the normalized value of the parameter</returns>
        public float MTPK_GetSynthParameterDefaultValue(fluid_gen_type genType)
        {
            float result = 0f;
            int genId = ConvertIdToIndex(genType);
            if (genId >= 0)
            {
                try
                {
                    result = Mathf.InverseLerp(fluid_gen_info.FluidGenInfo[genId].min, fluid_gen_info.FluidGenInfo[genId].max, fluid_gen_info.FluidGenInfo[genId].def);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"MTPK_GetSynthParameterDefaultValue - {ex.Message}");
                }
            }
            return result;
        }

        /// <summary>
        /// [Pro] V2.88.2 Get the label for the generator. 
        /// </summary>
        /// <param name="genType">see #MTPK_ModifySynthParameter</param>
        /// <returns>Return the label of the generator</returns>
        public static string MTPK_GetSynthParameterLabel(fluid_gen_type genType)
        {
            int genId = ConvertIdToIndex(genType);
            if (genId >= 0)
            {
                try
                {
                    GenModifier.InitListGenerator();
                    foreach (MPTKListItem item in GenModifier.RealTimeGenerator)
                        if (item.Index== genId)
                        {
                            return item.Label;// GenModifier.RealTimeGenerator
                        }

                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"MTPK_GetSynthParameterLabel - {ex.Message}");
                }
            }
            return "not found";
        }

        /// <summary>
        /// [Pro] V2.88.2 Get the list of modifiable generators.\n 
        /// It's a list of MPTKListItem where the properties are:\n
        ///! @li @c  MPTKListItem#Index : generator Id
        ///! @li @c  MPTKListItem#Label : texte associated to the generator
        ///! @li @c  MPTKListItem#Position : position in the list from 0
        /// </summary>
        /// <param name="genType">see #MTPK_ModifySynthParameter</param>
        /// <returns>Return a list of modifiable generator (don't modify this list!)</returns>
        public static List<MPTKListItem> MTPK_GetSynthParameterListGenerator()
        {
            GenModifier.InitListGenerator();
            return GenModifier.RealTimeGenerator;
        }

        /// <summary>
        /// [Pro] V2.88.2 Reset synth parameter to default soundfont value.
        /// </summary>
        public void MTPK_ClearSynthParameter()
        {
            GensModifier = null;
        }
    }
}
