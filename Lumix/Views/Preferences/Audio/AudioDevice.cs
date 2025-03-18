using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumix.Views.Preferences.Audio;

/// <summary>
/// Represents an audio device
/// </summary>
public class AudioDevice
{
    /// <summary>
    /// The audio driver used by this device
    /// </summary>
    public AudioDriver DriverType { get; private set; }

    /// <summary>
    /// The output device used by this instance
    /// </summary>
    public IWavePlayer OutputDevice { get; private set; }

    /// <summary>
    /// The name of this audio device
    /// </summary>
    public string DeviceName { get; private set; }

    /// <summary>
    /// Create a new ASIO device
    /// </summary>
    /// <param name="asioDevice"></param>
    public AudioDevice(AsioOut asioDevice)
    {
        OutputDevice = asioDevice;
        DeviceName = asioDevice.DriverName;
        DriverType = AudioDriver.Asio;
    }

    /// <summary>
    /// Create a new WASAPI device
    /// </summary>
    /// <param name="wasapiDevice"></param>
    /// <param name="exclusiveMode">Use exclusive mode to achieve lower latencies. External sounds won't be heard</param>
    /// <param name="latency"></param>
    public AudioDevice(MMDevice wasapiDevice, bool exclusiveMode = true)
    {
        OutputDevice = new WasapiOut(wasapiDevice, exclusiveMode ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared, 
            true, Convert.ToInt32(CoreAudioEngine.WasapiLatency));
        DeviceName = wasapiDevice.FriendlyName;
        DriverType = AudioDriver.Wasapi;
    }
}
