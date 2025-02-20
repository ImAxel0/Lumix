using ImGuiNET;
using Lumix.Views.Sidebar.Preview;

namespace Lumix.Views.Arrangement;

[Obsolete("Use TimeLineV2 instead")]
public class TimeLine
{
    private static float _currentTime;
    public static float CurrentTime => _currentTime;

    private static bool _isRunning;
    public static bool IsRunning => _isRunning;

    private static float _startTime;

    public static void StartTime()
    {
        _isRunning = true;
        _startTime = _currentTime;
        /*
        foreach (var track in ArrangementView.AudioTracks)
        {
            ArrangementView.MasterTrack.AudioEngine.AddAudioTrack(track.AudioEngine);
        }

        foreach (var track in ArrangementView.MidiTracks)
        {
            ArrangementView.MasterTrack.AudioEngine.AddMidiTrack(track.MidiEngine);
        }
        */
    }

    public static void StartRecording()
    {
        StartTime();
        foreach (var track in ArrangementView.Tracks)
        {
            if (track.RecordOnStart)
            {
                track.Engine.StartRecording();
            }
        }
    }

    public static void StopTime(bool moveToStart = false)
    {
        _isRunning = false;
        _currentTime = _startTime;
        if (moveToStart)
        {
            _startTime = 0;
            _currentTime = 0;
        }

        AudioPreviewEngine.Instance.StopSound();
        MidiPreviewEngine.StopPreview();
        foreach (var track in ArrangementView.Tracks)
        {
            track.Engine.StopSounds();
            foreach (var clip in track.Clips)
            {
                clip.HasPlayed = false;
            }

            if (track.Engine.IsRecording)
            {
                track.Engine.StopRecording(track);
            }
        }

        //ArrangementView.MasterTrack.AudioEngine.RemoveAllTracks();
    }

    public static float GetTimeInSeconds()
    {
        return _currentTime / TopBarControls.Bpm;
    }

    public static void SetTime(float time)
    {
        _currentTime = time;
    }

    public static void OnUpdate()
    {
        if (_isRunning)
        {
            _currentTime += ImGui.GetIO().DeltaTime * TopBarControls.Bpm;
        }
    }
}
