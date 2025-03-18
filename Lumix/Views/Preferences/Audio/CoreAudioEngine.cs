using Lumix.EventArguments;
using Lumix.Tracks.Master;
using Lumix.Views.Arrangement;
using Lumix.Views.Sidebar.Preview;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Lumix.Views.Preferences.Audio;

public enum AudioDriver
{
    Wasapi,
    Asio
}

public static class CoreAudioEngine
{
    private static bool _initialized;

    private static AudioDevice _audioDevice;
    public static AudioDevice AudioDevice
    {
        get => _audioDevice;
        private set
        {
            var old = _audioDevice;
            _audioDevice = value;
            AudioDeviceChanged?.Invoke(null, new AudioDeviceChangedEventArgs(old, value));
        }
    }

    public static int SampleRate { get; private set; } = 44100;

    public static event EventHandler<AudioDeviceChangedEventArgs> AudioDeviceChanged;

    /// <summary>
    /// Cached WASAPI devices since accessing FriendlyName is slow
    /// </summary>
    public static Dictionary<string, MMDevice?> WasapiDevices { get; private set; } = new();

    public static float WasapiLatency = 50;

    /// <summary>
    /// Initialize the audio engine with the first available audio device
    /// </summary>
    /// <returns></returns>
    public static bool Init()
    {
        if (_initialized) 
            return false;

        AudioDeviceChanged += AudioEngine_AudioDeviceChanged;

        // Find WASAPI devices
        var deviceEnumerator = new MMDeviceEnumerator();
        var devices = deviceEnumerator.EnumerateAudioEndPoints(
            DataFlow.Render,
            DeviceState.Active);

        // Cache them
        foreach (MMDevice device in devices)
        {
            WasapiDevices.Add(device.FriendlyName, device);
        }

#if LOCAL_DEV
        AudioDevice = new AudioDevice(new AsioOut("M-Audio AIR 192 4 ASIO"));
#else
        var wasapiOK = devices.Any();
        if (!wasapiOK)
        {
            if (AsioOut.isSupported())
            {
                AudioDevice = new AudioDevice(new AsioOut());
                return true;
            }
            User32.MessageBox(IntPtr.Zero, "No audio devices found.", "No Audio Device",
                User32.MB_FLAGS.MB_TOPMOST | User32.MB_FLAGS.MB_ICONWARNING);
            return false;
        }
        AudioDevice = new AudioDevice(devices.First());
#endif
        _initialized = true;
        return true;
    }

    /// <summary>
    /// Request a driver change and select the first available device of that driver type
    /// </summary>
    /// <param name="driver"></param>
    public static void ChangeDriver(AudioDriver driver)
    {
        if (driver == AudioDriver.Wasapi)
            AudioDevice = new AudioDevice(WasapiDevices.First().Value);
        else if (driver == AudioDriver.Asio)
            AudioDevice = new AudioDevice(new AsioOut());
    }

    /// <summary>
    /// Change used ASIO device
    /// </summary>
    /// <param name="asio"></param>
    public static void ChangeDevice(AsioOut asio)
    {
        AudioDevice = new AudioDevice(asio);
    }

    /// <summary>
    /// Change used WASAPI device
    /// </summary>
    /// <param name="asio"></param>
    public static void ChangeDevice(MMDevice wasapi)
    {
        AudioDevice = new AudioDevice(wasapi);
    }

    /// <summary>
    /// Change the used Sample Rate. 
    /// TODO: need to recreate all of the tracks engines with the new Sample Rate
    /// </summary>
    /// <param name="sampleRate"></param>
    public static void ChangeSampleRate(int sampleRate)
    {
        SampleRate = sampleRate;

        // Dispose and recreate the currently used audio device
        if (AudioDevice.DriverType == AudioDriver.Wasapi)
        {
            var wasapiDevice = WasapiDevices[AudioDevice.DeviceName];
            AudioDevice = new AudioDevice(wasapiDevice);
        }
        else if (AudioDevice.DriverType == AudioDriver.Asio)
        {
            var asio = new AsioOut(AudioDevice.DeviceName);
            AudioDevice = new AudioDevice(asio);
        }
    }

    /// <summary>
    /// Requested when WASAPI latency is changed in Audio settings
    /// </summary>
    public static void LatencyChanged()
    {
        if (AudioDevice.DriverType == AudioDriver.Wasapi)
        {
            var wasapiDevice = WasapiDevices[AudioDevice.DeviceName];
            AudioDevice = new AudioDevice(wasapiDevice, true);
        }
    }

    private static void RefreshEngine()
    {
        // We remove all tracks from the master mixer and create a new master track with the new audio device
        ArrangementView.MasterTrack.AudioEngine.RemoveAllTracks();
        ArrangementView.MasterTrack = new MasterTrack();

        AudioPreviewEngine.Instance = new AudioPreviewEngine(SampleRate); // We create the new audio preview instance

        // We readd all the tracks to the master mixer
        foreach (var track in ArrangementView.Tracks)
        {
            ArrangementView.MasterTrack.AudioEngine.AddTrack(track.Engine);
        }
    }

    private static void AudioEngine_AudioDeviceChanged(object? sender, AudioDeviceChangedEventArgs e)
    {
        e.OldAudioDevice?.OutputDevice?.Dispose();
        if (_initialized)
        {
            RefreshEngine();
        }
    }
}
