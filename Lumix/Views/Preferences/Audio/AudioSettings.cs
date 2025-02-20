using Lumix.Tracks.Master;
using Lumix.Views.Arrangement;
using Lumix.Views.Sidebar.Preview;
using NAudio.Wave;

namespace Lumix.Views.Preferences.Audio;

public enum AudioDriver
{
    WaveOut,
    Asio
}

public static class AudioSettings
{
    public static AudioDriver AudioDriver { get; private set; } = AudioDriver.WaveOut;
    public static string DeviceName { get; private set; } = string.Empty;
    public static IWavePlayer OutputDevice { get; private set; }
    public static IWavePlayer InputDevice { get; private set; }

    public static int SampleRate { get; private set; } = 44100;

    public static float WaveOutLatency = 100;

    public static void Init(bool driverChange = false)
    {
        //OutputDevice?.Stop();
        OutputDevice?.Dispose();

        if (AsioOut.GetDriverNames().Length > 0 && AudioDriver == AudioDriver.Asio)
        {
            var asio = new AsioOut(0);
            OutputDevice = asio;
            AudioDriver = AudioDriver.Asio;
            DeviceName = asio.DriverName;
        }
        else
        {
            var waveOut = new WaveOutEvent() { DesiredLatency = (int)WaveOutLatency };
            OutputDevice = waveOut;
            AudioDriver = AudioDriver.WaveOut;
            DeviceName = waveOut.DeviceNumber.ToString();
        }

        if (driverChange)
        {
            UpdateAudioOutput(OutputDevice, DeviceName);
        }
    }

    public static void UpdateAudioOutput(IWavePlayer outputDevice, string deviceName = "")
    {
        outputDevice.Stop();
        SetOutputDevice(outputDevice, deviceName);

        if (outputDevice is WaveOutEvent waveOut)
        {
            waveOut.DesiredLatency = (int)WaveOutLatency;
        }

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
            case AudioDriver.WaveOut:
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
