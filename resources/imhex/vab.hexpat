#pragma author pleonex
#pragma description PS audio container VAB

#pragma MIME application/x-psx-vab
#pragma magic [ 70 42 41 56 ] @ 0x00


#define MAX_PROGRAMS 0x80
#define MAX_TONES 0x10
#define MAX_WAVEFORMS 254

import type.magic;
import std.core;
import std.mem;

bool displayEmptyPrograms in;
bool displayEmptyTones in;

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
    
    // Hide empty programs
    if (!displayEmptyPrograms && toneCount == 0) {
        continue;
    }
} [[format("format_program")]];

fn format_program(VhProgramAttributes attr) {
  return std::format("[tones: {}]", attr.toneCount);
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

    if (!displayEmptyTones && waveformIndex == 0) {
        continue;
    }
} [[format("format_tone")]];

fn format_tone(VhToneAttributes attr) {
  return std::format("[program: #{}, waveform: #{}]", attr.programIndex, attr.waveformIndex);
};

// u16 wrapper for multiplying value by 8 (<< 3)
struct WaveformLength {
    u16 length;
} [[sealed, static, transform("transform_waveform_length"), format("transform_waveform_length")]];

fn transform_waveform_length(ref WaveformLength l) {
    u32 result = l.length << 3;
    return result;
};

struct Vh {
    VhHeader header;
    VhProgramAttributes programs[MAX_PROGRAMS];
    VhToneAttributes tones[MAX_TONES * header.programCount];
 
    // waveform lengths table
    padding[2]; // because waveformIndex is 1-based (for 0 as null) this is always 0
    WaveformLength waveformLengths[while(std::mem::read_unsigned($, 2) != 0)];
    padding[(MAX_WAVEFORMS - std::core::member_count(waveformLengths) + 1) * 2];
};

struct Vag {
    u32 length = parent.parent.vabHeader.waveformLengths[std::core::array_index()];
    u8 data[length];
} [[sealed]];

struct Vb {
    Vag waveForms[std::core::member_count(parent.vabHeader.waveformLengths)];
};

struct Vab {
    Vh vabHeader;
    Vb vabBody;
};


Vab vab @ 0x0000;