# VAB specification

_VAB_ (_very audio binary_?) is an audio format. It defines audio _tones_
grouped in _programs_. It also acts as a container of VAG audio files. Tones are
linked to a specific VAG audio. Multiple tones with different parameters may
point to the same audio.

Usually it has the extension `.VAB`. But it may also appear as two separated
files: the header with `.VH` extension and its body (VAG files) with `.VB`
extension.

## Limitations

The format have the following limitations:

- A maximum of 254 waveform files (VAG).
- Up to 128 audio programs with a maximum of 16 tones.
- The body (VB) or total size of waveform files (VAG) must be under 504 KB
  (`0x7E000`).
  - This is due to a limitation in the hardware of 512 KB in the SPU RAM.

## Format overview

The VAB format is divided in the following sections. The VH file will contain
everything except the last part of the body (VB file).

| Offset | Length   | Description                    |
| ------ | -------- | ------------------------------ |
| 0x0000 | 0x20     | Header information             |
| 0x0020 | 0x800    | Programs attributes            |
| 0x0820 | variable | Tones attributes               |
| ...    | 0x200    | Waveforms length table         |
| ...    | variable | Body with waveforms data (VAG) |

Quick formulas for variable offsets and lengths:

- Tones attributes length: `header.programs * 0x200`
- Offset to waveform length table: `0x820` + tones attributes length
- Header size: waveform length table offset + `0x200`
- Offset to body: header size

## Header (VH)

### Information

| Offset | Format  | Description                                               |
| ------ | ------- | --------------------------------------------------------- |
| 0x00   | char[4] | Format ID: `pBAV` (`VABp` in little-endian uint)          |
| 0x04   | uint    | Format version                                            |
| 0x08   | uint    | File ID                                                   |
| 0x0C   | uint    | Total size of header and body (even in header only files) |
| 0x10   | ushort  | Reserved                                                  |
| 0x12   | ushort  | Number of program attributes                              |
| 0x14   | ushort  | Total number of tone attributes                           |
| 0x16   | ushort  | Number of waveforms in body (VAG)                         |
| 0x18   | byte    | Master volume                                             |
| 0x19   | byte    | Master pan                                                |
| 0x1A   | byte    | User-defined bank attribute 1                             |
| 0x1B   | byte    | User-defined bank attribute 2                             |
| 0x1C   | uint    | Reserved                                                  |

### Program attributes

After the header information follows a list of program attribute definitions.
There are always 128 program entries in the format, although only a few of them
will be filled. There are always 16 bytes per entry.

| Offset | Format | Description                          |
| ------ | ------ | ------------------------------------ |
| 0x00   | byte   | Number of valid tones in the program |
| 0x01   | byte   | Master volume for tones              |
| 0x02   | byte   | Priority of tones                    |
| 0x03   | byte   | Mode, default: `0xE0`                |
| 0x04   | byte   | Master panning                       |
| 0x05   | byte   | Reserved                             |
| 0x06   | ushort | Additional attributes                |
| 0x08   | uint   | Reserved, default: `-1`              |
| 0x0C   | uint   | Reserved, default: `-1`              |

#### Empty programs

_Empty_ programs do not count towards the count in the header and will have a
tone count of `0`. They may appear in any position of the program list, not
necessarily always at the end (e.g., between valid programs).

Usually an _empty_ program repeats the same values of the last valid one
(reference program), except for the following fields:

- Tone count: always 0.
- Master volume: reference program value or 0 for format version 6 and higher.
- Master panning: reference program value or 0 for format version 6 and higher.

Note that some programs that do not appear at the end, may have unpredictable
values. Most likely they were filled at some point of the development process,
but they end up with no tones.

### Tone attributes

After the list of program attributes, there is a list of tones attributes in
order of programs. There are always 16 tones for each program, although as
happen for programs only a few will be not empty. A tone attribute entry has
always 32 bytes.

| Offset | Format    | Description                                  |
| ------ | --------- | -------------------------------------------- |
| 0x00   | byte      | Priority                                     |
| 0x01   | byte      | Mode, 0: normal, 4: reverberation            |
| 0x02   | byte      | Tone volume                                  |
| 0x03   | byte      | Tone panning                                 |
| 0x04   | byte      | Centre tone in semitone units                |
| 0x05   | byte      | Centre note fine-tuning for pitch correction |
| 0x06   | byte      | Note minimum value                           |
| 0x07   | byte      | Note maximum value                           |
| 0x08   | byte      | Vibration width                              |
| 0x09   | byte      | Vibration duration                           |
| 0x0A   | byte      | Portamento width                             |
| 0x0B   | byte      | Portamento duration                          |
| 0x0C   | byte      | Minimum pitch bend                           |
| 0x0D   | byte      | Maximum pitch bend                           |
| 0x0E   | byte      | Reserved                                     |
| 0x0F   | byte      | Reserved                                     |
| 0x10   | ushort    | Envelope settings for attack and decay       |
| 0x12   | ushort    | Envelope settings for release and sustain    |
| 0x14   | ushort    | Program index (0-based)                      |
| 0x16   | ushort    | Waveform index (1-based), 0 for empty tone   |
| 0x18   | ushort[4] | Reserved                                     |

#### Empty tones

Similar to the empty programs, tones may be empty and be just a placeholder so
it's easier to calculate offsets. Note that an _empty program_ do not have any
tone definitions, as long as a program defines one tone, there will be 16 in the
format.

_Empty_ tones do not count towards the count in program attributes and will have
a waveform index equal to `0`, except for VAB format 5. In that case process the
tones in order with the count of program attributes, the rest would be empty.
Empty tones may appear in any position of the list, not necessarily always at
the end (e.g., between valid tones, good luck detecting for format 5).

The content of an empty tone changes depending on the format version:

- Fields `0x00` to `0x0E`:
  - Version 5 (and lower?): reference tone values
  - Version 6 and higher: `0x00` byte filled
  - Except for note minimum and maximum values that are always `0x00`.
- Reserved fields: reference tone values
- Envelope settings:
  - Version 5 (and lower?): reference tone
  - Version 6: `0x00` byte filled
  - Version 7 and higher: first: `0x80FF`, second: `0x5FC0`.
- Waveform index:
  - Version 5 (and lower?): reference tone value
  - Version 6 and higher: `0x00`.

Note that some tones that do not appear at the end, may have unpredictable
values. Most likely they were filled at some point of the development process,
but the linked waveform file was removed.

### Waveform length table

The last part of the header is a table with the waveform (VAG) data lengths.
Each length value is a 16-bits unsigned integer 3-bits right-shifted (multiply
by 8 the format value).

The first value should be ignored, and it is always `0x00`. Probably due to
waveform index being 1-based in the tone attributes.

The table is always `0x200` bytes long. The remaining bytes are `0x00` filled.

To calculate a waveform offset, sum the lengths of the previous files.

## Body (VB)

The body contains a set of waveform audio files. The format is `VAG` **without
header**. They usually start with 16-bytes `0x00`.
