using Jacobi.Vst.Core;
using Jacobi.Vst.Host.Interop;
using Lumix.Views.Midi;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Veldrid.Sdl2;

namespace Lumix.Plugins.VST;

public enum VstType
{
    /// <summary>VST is an effect</summary>
    VST,
    /// <summary>VST is an instrument</summary>
    VSTi
}

public class VstPlugin
{
    private string _pluginId = Guid.NewGuid().ToString();
    public string PluginId => _pluginId;

    private string _pluginName;
    public string PluginName => _pluginName;

    private VstType _pluginType;
    public VstType PluginType => _pluginType;

    private VstPluginContext _pluginContext;
    public VstPluginContext PluginContext => _pluginContext;

    private Sdl2Window _pluginWindow;
    public Sdl2Window PluginWindow => _pluginWindow;

    public VstPlugin(string pluginPath)
    {
        _pluginContext = LoadPlugin(pluginPath);
        _pluginName = Path.GetFileNameWithoutExtension(pluginPath);
        _pluginType = _pluginContext.PluginInfo.Flags.HasFlag(VstPluginFlags.IsSynth) ? VstType.VSTi : VstType.VST;
    }

    public void SendNoteOn(int channel, int note, int velocity)
    {
        if (_pluginContext == null) return;

        var midiEvent = new VstMidiEvent(
            deltaFrames: 0,               // When the event occurs (relative to the current processing block)
            noteLength: 0,                // Duration of the note (optional, can be 0)
            noteOffset: 0,                // Offset within the note (optional, can be 0)
            midiData: new byte[]
            {
                (byte)(0x90 | channel & 0x0F),  // Note On status byte (0x90) + channel
                (byte)(note & 0x7F),             // Note number (0-127)
                (byte)(velocity & 0x7F)          // Velocity (0-127)
            },
            detune: 0,
            noteOffVelocity: 0);

        _pluginContext.PluginCommandStub.Commands.ProcessEvents(new VstEvent[] { midiEvent });
    }

    public void SendNoteOff(int channel, int note, int velocity)
    {
        if (_pluginContext == null) return;

        var midiEvent = new VstMidiEvent(
            deltaFrames: 0,
            noteLength: 0,
            noteOffset: 0,
            midiData: new byte[]
            {
                (byte)(0x80 | channel & 0x0F),  // Note Off status byte (0x80) + channel
                (byte)(note & 0x7F),             // Note number (0-127)
                (byte)(velocity & 0x7F)          // Release velocity (0-127)
            },
            detune: 0,
            noteOffVelocity: 0);

        _pluginContext.PluginCommandStub.Commands.ProcessEvents(new VstEvent[] { midiEvent });
    }

    public void SendSustainPedal(int channel, bool isPressed)
    {
        if (_pluginContext == null) return;

        var midiEvent = new VstMidiEvent(
            deltaFrames: 0,               // When the event occurs (relative to the current processing block)
            noteLength: 0,                // Duration (not applicable for CC messages)
            noteOffset: 0,                // Offset within the event (optional, can be 0)
            midiData: new byte[]
            {
            (byte)(0xB0 | (channel & 0x0F)),  // Control Change status byte (0xB0) + channel
            (byte)(64),                      // Controller number for sustain pedal
            (byte)(isPressed ? 127 : 0)      // Controller value (127 for on, 0 for off)
            },
            detune: 0,
            noteOffVelocity: 0);

        _pluginContext.PluginCommandStub.Commands.ProcessEvents(new VstEvent[] { midiEvent });
    }


    private void HostCmdStub_PluginCalled(object sender, PluginCalledEventArgs e)
    {
        var hostCmdStub = (HostCommandStub)sender;

        // can be null when called from inside the plugin main entry point.
        if (hostCmdStub.PluginContext.PluginInfo != null)
        {
            Console.WriteLine("Plugin " + hostCmdStub.PluginContext.PluginInfo.PluginID + " called:" + e.Message);
        }
        else
        {
            Console.WriteLine("The loading Plugin called:" + e.Message);
        }
    }

    private void HostCmdStub_SizeWindow(object sender, SizeWindowEventArgs e)
    {
        _pluginWindow.Width = e.Width;
        _pluginWindow.Height = e.Height;
    }

    private VstPluginContext LoadPlugin(string pluginPath)
    {
        try
        {
            var hostCmdStub = new HostCommandStub();
            hostCmdStub.PluginCalled += new EventHandler<PluginCalledEventArgs>(HostCmdStub_PluginCalled);
            hostCmdStub.SizeWindow += new EventHandler<SizeWindowEventArgs>(HostCmdStub_SizeWindow);
            var ctx = VstPluginContext.Create(pluginPath, hostCmdStub);

            // add custom data to the context
            ctx.Set("PluginPath", pluginPath);
            ctx.Set("HostCmdStub", hostCmdStub);

            // actually open the plugin itself
            ctx.PluginCommandStub.Commands.Open();

            // We check if plugin returns rect data; if it doesn't we try to populate it with by opening the editor with dummy handle
            var rect = ctx.PluginCommandStub.Commands.EditorGetRect(out var rectangle);
            System.Drawing.Rectangle pluginRect = new();
            bool rectWasFound = rect;
            if (!rectWasFound)
            {
                ctx.PluginCommandStub.Commands.EditorOpen(IntPtr.Zero); // Open dummy editor, may works for some plugins to populate the rectangle data
                rect = ctx.PluginCommandStub.Commands.EditorGetRect(out var dummyRect);
                ctx.PluginCommandStub.Commands.EditorClose(); // Destroy the dummy editor
                pluginRect = dummyRect;
            }
            else
                pluginRect = rectangle;

            // Check if the plugin has an editor
            if (rect)
            {
                // Create a host window for the editor
                string windowTitle = Path.GetFileNameWithoutExtension(pluginPath);
                IntPtr hwnd = CreateWindow(windowTitle, pluginRect.Width, pluginRect.Height);

                // Attach the editor to the window
                ctx.PluginCommandStub.Commands.EditorOpen(hwnd);

                StartEditorIdle();
                Console.WriteLine("Plugin editor opened successfully.");
            }
            else
            {
                Console.WriteLine("The plugin does not have an editor.");
            }

            return ctx;
        }
        catch (Exception e)
        {
            User32.MessageBox(IntPtr.Zero, e.ToString(), e.Message);
        }

        return null;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_STYLE = -16;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private const int WS_THICKFRAME = 0x00040000;
    private void RemoveMinimizeAndMaximizeButtons(IntPtr windowHandle)
    {
        int style = GetWindowLong(windowHandle, GWL_STYLE);
        style &= ~WS_MINIMIZEBOX; // Remove minimize button
        style &= ~WS_MAXIMIZEBOX; // Remove maximize button
        style &= ~WS_THICKFRAME; // Make the window non-resizable (except from plugin controls)
        SetWindowLong(windowHandle, GWL_STYLE, style);
    }

    private IntPtr CreateWindow(string title, int width, int height)
    {
        _pluginWindow = new Sdl2Window(title, 400, 400, width, height, SDL_WindowFlags.AlwaysOnTop | SDL_WindowFlags.Resizable | SDL_WindowFlags.SkipTaskbar, false);
        _pluginWindow.Closing += () =>
        {
            if (_pluginWindow.Exists)
            {
                _pluginContext.PluginCommandStub?.Commands.EditorClose();
            }
        };

        // These allows the plugin window to communicate with the main window
        _pluginWindow.KeyDown += VirtualKeyboard.KeyDownFromPlugin;
        _pluginWindow.KeyUp += VirtualKeyboard.KeyUpFromPlugin;

        // Make window always stay on top
        User32.SetWindowPos(_pluginWindow.Handle, HWND.HWND_TOPMOST, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE
            | User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_SHOWWINDOW);

        RemoveMinimizeAndMaximizeButtons(_pluginWindow.Handle);

        return _pluginWindow.Handle;
    }

    /// <summary>
    /// Makes the plugin ui updated accordingly to control changes
    /// </summary>
    private void StartEditorIdle()
    {
        Task.Run(async () =>
        {
            while (_pluginWindow.Exists)
            {
                _pluginContext?.PluginCommandStub.Commands.EditorIdle();
                await Task.Delay(16);
            }
        });
    }

    private void RecreateWindow()
    {
        // Check if the plugin has an editor
        var rect = _pluginContext.PluginCommandStub.Commands.EditorGetRect(out var rectange);
        if (rect)
        {
            // Create a host window for the editor
            string windowTitle = Path.GetFileNameWithoutExtension(_pluginContext.Find<string>("PluginPath"));
            IntPtr hwnd = CreateWindow(windowTitle, rectange.Width, rectange.Height);

            // Attach the editor to the window
            _pluginContext.PluginCommandStub.Commands.EditorOpen(hwnd);

            StartEditorIdle();
            Console.WriteLine("Plugin editor opened successfully.");
        }
        else
        {
            Console.WriteLine("The plugin does not have an editor.");
        }
    }

    public void OpenPluginWindow()
    {
        if (!_pluginWindow.Exists)
        {
            int x = _pluginWindow.X;
            int y = _pluginWindow.Y;
            RecreateWindow();
            _pluginWindow.X = x;
            _pluginWindow.Y = y;
        }
    }

    public void Dispose(bool closeWindow = true)
    {
        if (closeWindow)
        {
            _pluginWindow?.Close();
        }
        _pluginContext?.Dispose();
    }
}
