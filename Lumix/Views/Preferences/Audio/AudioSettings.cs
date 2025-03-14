using Lumix.Tracks.Master;
using Lumix.Views.Arrangement;
using Lumix.Views.Sidebar.Preview;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Lumix.Views.Preferences.Audio;

public enum AudioDriver
{
    Wasapi,
    Asio
}

public static class AudioSettings
{
    public static AudioDriver AudioDriver { get; private set; } = AudioDriver.Wasapi;
    public static string DeviceName { get; private set; } = string.Empty;
    public static IWavePlayer OutputDevice { get; private set; }
    public static IWavePlayer InputDevice { get; private set; }

    /// <summary>
    /// FriendlyName, MMDevice
    /// </summary>
    public static Dictionary<string, MMDevice?> WasapiDevices { get; private set; } = new();

    public static float WasapiLatency = 50;

    public static int SampleRate { get; private set; } = 44100;

    public static void Init(bool driverChange = false)
    {
        OutputDevice?.Dispose();

#if LOCAL_DEV
        var asio = new AsioOut("M-Audio AIR 192 4 ASIO");
        OutputDevice = asio;
        AudioDriver = AudioDriver.Asio;
        DeviceName = asio.DriverName;
#else
        if (AsioOut.GetDriverNames().Length > 0 && AudioDriver == AudioDriver.Asio)
        {
            var asio = new AsioOut(0);
            OutputDevice = asio;
            AudioDriver = AudioDriver.Asio;
            DeviceName = asio.DriverName;
        }
        else if (AudioDriver == AudioDriver.Wasapi)
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (MMDevice device in devices)
            {
                WasapiDevices.Add(device.FriendlyName, device);
            }
            var wasapiOut = new WasapiOut(devices[0], AudioClientShareMode.Exclusive, true, (int)WasapiLatency);
            OutputDevice = wasapiOut;
            AudioDriver = AudioDriver.Wasapi;
            DeviceName = devices[0].FriendlyName;
        }
#endif
        if (driverChange)
        {
            UpdateAudioOutput(OutputDevice, DeviceName);
        }
    }

    public static void UpdateAudioOutput(IWavePlayer outputDevice, string deviceName = "")
    {
        outputDevice.Stop();
        SetOutputDevice(outputDevice, deviceName);

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

    public static void SetSampleRate(int sampleRate)
    {
        SampleRate = sampleRate;
    }

    public static void SetDriver(AudioDriver audioDriver)
    {
        AudioDriver = audioDriver;
    }

    private static void SetOutputDevice(IWavePlayer outputDevice, string deviceName = "")
    {
        switch (AudioDriver)
        {
            case AudioDriver.Wasapi:
                OutputDevice = outputDevice;
                DeviceName = deviceName;
                break;
            case AudioDriver.Asio:
                OutputDevice = outputDevice;
                DeviceName = deviceName;
                break;
        }

        //ArrangementView.MasterTrack = new MasterTrack();
    }
}
