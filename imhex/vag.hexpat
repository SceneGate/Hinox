#pragma author pleonex
#pragma description PS audio container VAB

#pragma MIME application/x-psx-vab
#pragma magic [ 70 42 41 56 ] @ 0x00


#define MAX_PROGRAMS 0x80
#define MAX_TONES 0x10
#define MAX_WAVEFORMS 254

import type.magic;
import std.core;

bool displayEmptyPrograms in;
bool displayEmptyTones in;
u32 waveformsCount out;

struct VhHeader {
  type::Magic<"pBAV"> fileId;
  u32 version;
  u32 vabId;
  u32 vabSize;
  u16 reserved0;
  
  u16 programCount;
  u16 toneCount;
  u16 waveformCount;
  
  u8 masterVolume;
  u8 masterPan;
  u8 bankAttributes1;
  u8 bankAttributes2;
  u32 reserved1;
};

struct VhProgramAttributes {
    u8 toneCount;
    u8 masterVolume;
    u8 priority;
    u8 mode;
    u8 masterPanning;
    u8 reserved0;
    u16 attributes;
    u32 reserved1;
    u32 reserved2;
    
    if (!displayEmptyPrograms && toneCount == 0) {
        continue;
    }
} [[format("get_program_name")]];

fn get_program_name(VhProgramAttributes attr) {
  return std::format("tones {}", attr.toneCount);
};

struct VhToneAttributes {
    u8 priority;
    u8 mode;
    u8 volume;
    u8 panning;
    u8 centre;
    u8 fine;
    u8 minimum;
    u8 maximum;
    u8 vibrationWidth;
    u8 vibrationTime;
    u8 portamentoWidth;
    u8 portamentoTime;
    u8 pitchBendMinimum;
    u8 pitchBendMaximum;
    u8 reserved0;
    u8 reserved1;
    u16 envelopeSettings1;
    u16 envelopeSettings2;
    u16 programIndex;
    u16 waveformIndex; // starting at 1, 0 for invalid tone
    u64 reserved2;
    
    if (waveformIndex > waveformsCount) {
        waveformsCount = waveformIndex;
    }
    
    if (!displayEmptyTones && waveformIndex == 0) {
        continue;
    }
} [[format("get_tone_name")]];

fn get_tone_name(VhToneAttributes attr) {
  return std::format("program {}, waveform {}", attr.programIndex, attr.waveformIndex);
};

struct WaveformLength {
    u16 length;
} [[sealed, static, transform("lsh3"), format("lsh3")]];

fn lsh3(ref WaveformLength l) {
    u32 result = l.length << 3;
    return result;
};

struct Vh {
    VhHeader header;
    VhProgramAttributes programs[MAX_PROGRAMS];
    
    // This includes empty tones. Use program.toneCount for each program
    VhToneAttributes tones[MAX_TONES * header.programCount];
 
    padding[2]; // because waveformIndex is 1-based (for 0 as null) this is always 0
    WaveformLength waveformLengths[waveformsCount];
    padding[(MAX_WAVEFORMS - waveformsCount + 1) * 2];
};

struct Vag {
    u32 length = parent.parent.vabHeader.waveformLengths[std::core::array_index()];
    u8 data[length];
} [[sealed]];

struct Vb {
    Vag waveForm[waveformsCount];
};

struct Vab {
    Vh vabHeader;
    Vb vabBody;
};


Vab vab @ 0x0000;
std::print("Files {}", waveformsCount);