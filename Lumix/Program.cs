using ImGuiNET;
using System.Diagnostics;
using System.Numerics;
using Vanara.PInvoke;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Lumix.Views;
using Lumix.Views.Sidebar;
using Lumix.ImGuiExtensions;
using Lumix.Views.Arrangement;
using Lumix.Views.Midi;
using Lumix.Views.Preferences;

internal class Program
{
    public static Sdl2Window _window;
    public static GraphicsDevice _gd;
    public static ImGuiController _controller;
    private static CommandList _cl;
    private static Vector3 _clearColor = new(0.45f, 0.55f, 0.6f);
    private static Vector2 _minWindowSize = new(1280, 720);

    [STAThread]
    private static void Main(string[] args)
    {
        User32.SetProcessDPIAware();
        
        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(50, 50, 1280, 720, WindowState.Maximized, $"Lumix"),
            new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true),
            out _window,
            out _gd);
        _window.Resized += () =>
        {
            if (_window.Width < _minWindowSize.X)
                _window.Width = (int)_minWindowSize.X;

            if (_window.Height < _minWindowSize.Y)
                _window.Height = (int)_minWindowSize.Y;

            _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
            _controller.WindowResized(_window.Width, _window.Height);
        };

        _cl = _gd.ResourceFactory.CreateCommandList();
        _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

        var stopwatch = Stopwatch.StartNew();
        float deltaTime = 0f;

        ImGuiTheme.PushGreyTheme();
        ArrangementView.Init();

        while (_window.Exists)
        {
            deltaTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
            stopwatch.Restart();
            InputSnapshot snapshot = _window.PumpEvents();
            if (!_window.Exists) { break; }
            _controller.Update(deltaTime, snapshot);

            if (ImGui.IsKeyPressed(ImGuiKey.F11, false))
            {
                var windowsState = _window.WindowState == WindowState.BorderlessFullScreen ? WindowState.Normal : WindowState.BorderlessFullScreen;
                _window.WindowState = windowsState;
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Space, false))
            {
                if (TimeLineV2.IsPlaying())
                    TimeLineV2.StopPlayback();
                else
                    TimeLineV2.StartPlayback();
            }

            if (VirtualKeyboard.Enabled)
                VirtualKeyboard.ListenForKeyPresses();

            RenderUI();

#if DEBUG
            Lumix.Views.Debug.Render();
#endif

            ImGuiController.UpdateMouseCursor();

            _cl.Begin();
            _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
            _controller.Render(_gd, _cl);
            _cl.End();
            _gd.SubmitCommands(_cl);
            _gd.SwapBuffers(_gd.MainSwapchain);

            if (_window.WindowState == WindowState.Minimized)
            {
                Thread.Sleep(100);
                continue;
            }
        }

        _gd.WaitForIdle();
        _controller.Dispose();
        _cl.Dispose();
        _gd.Dispose();
        Process.GetCurrentProcess().Kill(); // temporary solution since process doesn't close when using ASIO4ALL
    }

    private static void RenderUI()
    {
        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Once);
        ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
        //ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new Vector4(0.20f, 0.22f, 0.27f, 1.00f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.Begin("Main", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoBringToFrontOnFocus);
        ImGui.PopStyleVar();

        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New"))
                {

                }

                if (ImGui.MenuItem("Open"))
                {

                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.MenuItem("Fullscreen", "", _window.WindowState == WindowState.BorderlessFullScreen))
                {
                    _window.WindowState = _window.WindowState == WindowState.BorderlessFullScreen ? WindowState.Normal : WindowState.BorderlessFullScreen;
                }

                if (ImGui.MenuItem("Debug", "", Lumix.Views.Debug._show))
                {
                    Lumix.Views.Debug._show = !Lumix.Views.Debug._show;
                }

                if (ImGui.MenuItem("Preferences", "Ctrl+;", PreferencesView.ShowView))
                {
                    PreferencesView.ShowView = !PreferencesView.ShowView;
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }
        //ImGui.PopStyleColor();

        TopBarControls.Render();

        float heightPercentage = BottomView.RenderedWindow == BottomViewWindows.DevicesView ? 75f : 40f;
        float height = Math.Clamp(ImGui.GetIO().DisplaySize.Y * heightPercentage / 100, 0, ImGui.GetContentRegionAvail().Y - 250);
        if (BottomView.RenderedWindow == BottomViewWindows.DevicesView)
            height = Math.Clamp(height, ImGui.GetIO().DisplaySize.Y - 350, ImGui.GetIO().DisplaySize.Y);

        if (ImGui.BeginChild("main_columns", new(ImGui.GetIO().DisplaySize.X, height)))
        {
            SidebarView.Render();
            ImGui.SameLine();
            ArrangementView.Render();
            ImGui.EndChild();
        }

        ImGui.Spacing();
        ImGui.Spacing();

        switch (BottomView.RenderedWindow)
        {
            case BottomViewWindows.DevicesView:
                InfoBox.Render();
                ImGui.SameLine();
                DevicesView.Render();
                break;
            case BottomViewWindows.MidiClipView:
                InfoBox.Render();
                ImGui.SameLine();
                MidiClipView.Render();
                break;
            case BottomViewWindows.AudioClipView:
                break;
        }

        if (PreferencesView.ShowView)
            PreferencesView.Render();

        ImGui.End();
    }
}