using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumix.Views.Preferences.Audio;

namespace Lumix.EventArguments;

public class AudioDeviceChangedEventArgs : EventArgs
{
    public AudioDevice OldAudioDevice { get; set; }
    public AudioDevice NewAudioDevice { get; set; }

    public AudioDeviceChangedEventArgs(AudioDevice oldAudioDevice, AudioDevice newAudioDevice)
    {
        OldAudioDevice = oldAudioDevice;
        NewAudioDevice = newAudioDevice;
    }
}
