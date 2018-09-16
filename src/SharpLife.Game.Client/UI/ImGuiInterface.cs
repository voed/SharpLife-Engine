/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using ImGuiNET;
using Serilog;
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Engine.Shared.API.Engine.Client;
using SharpLife.Engine.Shared.API.Game.Client;
using SharpLife.Engine.Shared.Logging;
using SharpLife.Renderer;
using SharpLife.Renderer.Utility;
using SharpLife.Utility;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpLife.Game.Client.UI
{
    public sealed class ImGuiInterface : IClientUI, ILogListener
    {
        /// <summary>
        /// In order to handle text addition properly, we have to track the state across frames
        /// When added, the next frame won't scroll yet, since the content size is out of date
        /// </summary>
        private enum TextAdded
        {
            No = 0,
            Yes,
            ApplyScroll
        }

        private readonly FrameTimeAverager _fta = new FrameTimeAverager(0.666);

        private readonly ILogger _logger;

        private readonly IClientEngine _engine;

        private bool _consoleVisible;

        private bool _materialControlVisible;

        private string _consoleText = string.Empty;

        private const int _maxConsoleChars = ushort.MaxValue;

        private byte[] _consoleTextUTF8;

        private TextAdded _textAdded;

        private readonly byte[] _consoleInputBuffer = new byte[1024];

        private IVariable _fpsMax;
        private IVariable _mainGamma;
        private IVariable _textureGamma;
        private IVariable _lightingGamma;
        private IVariable _brightness;
        private IVariable _overbright;
        private IVariable _fullbright;
        private IVariable _maxSize;
        private IVariable _roundDown;
        private IVariable _picMip;

        public ImGuiInterface(ILogger logger, IClientEngine engine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            _engine.LogListener = this;
        }

        public void Update(float deltaSeconds, IViewState viewState)
        {
            _fta.AddTime(deltaSeconds);
        }

        public void Draw(IViewState viewState)
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("Tools"))
                {
                    ImGui.Checkbox("Toggle Console", ref _consoleVisible);

                    ImGui.Checkbox("Toggle Material Control", ref _materialControlVisible);

                    ImGui.EndMenu();
                }

                ImGui.Text(_fta.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + _fta.CurrentAverageFrameTimeMilliseconds.ToString("#00.00 ms"));

                ImGui.TextUnformatted($"Camera Position: {viewState.Origin} Camera Angles: Pitch {viewState.Angles.X} Yaw {viewState.Angles.Y}");

                ImGui.EndMainMenuBar();
            }

            DrawConsole();

            DrawMaterialControl();
        }

        public void Write(char value)
        {
            _consoleText += value;

            _textAdded = TextAdded.Yes;

            TruncateConsoleText();
        }

        public void Write(char[] buffer, int index, int count)
        {
            var text = new string(buffer, index, count);
            _consoleText += text;

            _textAdded = TextAdded.Yes;

            TruncateConsoleText();
        }

        private void TruncateConsoleText()
        {
            if (_consoleText.Length > _maxConsoleChars)
            {
                _consoleText = _consoleText.Substring(_consoleText.Length - _maxConsoleChars);
            }
        }

        private void DrawConsole()
        {
            if (_consoleVisible && ImGui.BeginWindow("Console", ref _consoleVisible, WindowFlags.NoCollapse))
            {
                var textHeight = ImGuiNative.igGetTextLineHeight();

                var contentMin = ImGui.GetWindowContentRegionMin();
                var contentMax = ImGui.GetWindowContentRegionMax();

                var wrapWidth = contentMax.X - contentMin.X;
                //Leave some space at the bottom
                var maxHeight = contentMax.Y - contentMin.Y - (textHeight * 3);

                //Convert the text to UTF8
                //Acount for null terminator
                var byteCount = Encoding.UTF8.GetByteCount(_consoleText) + 1;

                //Resize on demand
                if (_consoleTextUTF8 == null || _consoleTextUTF8.Length < byteCount)
                {
                    _consoleTextUTF8 = new byte[byteCount];
                }

                var bytesWritten = StringUtils.EncodeNullTerminatedString(Encoding.UTF8, _consoleText, _consoleTextUTF8);

                //Display as input text area to allow text selection
                var pinnedBuffer = GCHandle.Alloc(_consoleTextUTF8, GCHandleType.Pinned);
                var bufferAddress = pinnedBuffer.AddrOfPinnedObject();

                ImGui.InputTextMultiline("##consoleText", bufferAddress, (uint)bytesWritten + 1, new Vector2(wrapWidth, maxHeight), InputTextFlags.ReadOnly, null);

                //Scroll to bottom when new text is added
                ImGuiNative.igBeginGroup();
                var id = ImGui.GetID("##consoleText");
                ImGui.BeginChildFrame(id, new Vector2(wrapWidth, maxHeight), WindowFlags.Default);

                switch (_textAdded)
                {
                    case TextAdded.Yes:
                        {
                            _textAdded = TextAdded.ApplyScroll;
                            break;
                        }

                    case TextAdded.ApplyScroll:
                        {
                            _textAdded = TextAdded.No;

                            var yPos = ImGuiNative.igGetScrollMaxY();

                            ImGuiNative.igSetScrollY(yPos);
                            break;
                        }
                }

                ImGui.EndChildFrame();
                ImGuiNative.igEndGroup();

                pinnedBuffer.Free();

                ImGui.Text("Command:");
                ImGui.SameLine();

                //Max width for the input
                ImGui.PushItemWidth(-1);

                if (ImGui.InputText("##consoleInput", _consoleInputBuffer, (uint)_consoleInputBuffer.Length, InputTextFlags.EnterReturnsTrue, null))
                {
                    //Needed since GetString doesn't understand null terminated strings
                    var stringLength = StringUtils.NullTerminatedByteLength(_consoleInputBuffer);

                    if (stringLength > 0)
                    {
                        var commandText = Encoding.UTF8.GetString(_consoleInputBuffer, 0, stringLength);
                        _logger.Information($"] {commandText}");
                        _engine.CommandContext.QueueCommands(commandText);
                    }

                    Array.Fill<byte>(_consoleInputBuffer, 0, 0, stringLength);
                }

                ImGui.PopItemWidth();

                ImGui.EndWindow();
            }
        }

        private void CacheVariable(ref IVariable variable, string name)
        {
            if (variable == null)
            {
                variable = _engine.CommandContext.FindCommand<IVariable>(name) ?? throw new NotSupportedException($"Couldn't get console variable {name}");
            }
        }

        private void CacheConsoleVariables()
        {
            CacheVariable(ref _fpsMax, "fps_max");
            CacheVariable(ref _mainGamma, "mat_gamma");
            CacheVariable(ref _textureGamma, "mat_texgamma");
            CacheVariable(ref _lightingGamma, "mat_lightgamma");
            CacheVariable(ref _brightness, "mat_brightness");
            CacheVariable(ref _overbright, "mat_overbright");
            CacheVariable(ref _fullbright, "mat_fullbright");
            CacheVariable(ref _maxSize, "mat_max_size");
            CacheVariable(ref _roundDown, "mat_round_down");
            CacheVariable(ref _picMip, "mat_picmip");
        }

        private void DrawIntSlider(IVariable variable, string sliderLabel, int fallbackMin, int fallbackMax, string displayText, Func<IVariable, int> getValue, Action<IVariable, int> setValue)
        {
            var minMaxFilter = variable.GetFilter<MinMaxFilter>();

            if (minMaxFilter != null)
            {
                var value = getValue(variable);

                if (ImGui.SliderInt(sliderLabel, ref value, (int)(minMaxFilter.Min ?? fallbackMin), (int)(minMaxFilter.Max ?? fallbackMax), displayText))
                {
                    setValue(variable, value);
                }
            }
        }

        private void DrawIntSlider(IVariable variable, string sliderLabel, int fallbackMin, int fallbackMax, string displayText)
        {
            DrawIntSlider(variable, sliderLabel, fallbackMin, fallbackMax, displayText, var => var.Integer, (var, value) => var.Integer = value);
        }

        private void DrawFloatSlider(IVariable variable, string sliderLabel, float fallbackMin, float fallbackMax, string displayText)
        {
            var minMaxFilter = variable.GetFilter<MinMaxFilter>();

            if (minMaxFilter != null)
            {
                var value = variable.Float;

                if (ImGui.SliderFloat(sliderLabel, ref value, minMaxFilter.Min ?? fallbackMin, minMaxFilter.Max ?? fallbackMax, displayText, 1))
                {
                    variable.Float = value;
                }
            }
        }

        private void DrawCheckbox(IVariable variable, string label)
        {
            var value = variable.Boolean;

            if (ImGui.Checkbox(label, ref value))
            {
                variable.Boolean = value;
            }
        }

        private void DrawMaterialControl()
        {
            if (_materialControlVisible && ImGui.BeginWindow("Material Control", ref _materialControlVisible, WindowFlags.NoCollapse))
            {
                CacheConsoleVariables();

                DrawIntSlider(_fpsMax, "Maximum Frames Per Second", 0, 1000, "%d FPS");

                DrawFloatSlider(_mainGamma, "Main Gamma", 0, 10, "%0.1f");
                DrawFloatSlider(_textureGamma, "Texture Gamma", 0, 10, "%0.1f");
                DrawFloatSlider(_lightingGamma, "Lighting Gamma", 0, 10, "%0.1f");
                DrawFloatSlider(_brightness, "Brightness Override", 0, 10, "%0.1f");

                DrawIntSlider(_maxSize, "Constrain texture scales to this maximum size", ImageConversionUtils.MinimumMaxTextureSize, 1 << 14, "%d");
                DrawIntSlider(_roundDown, "Round Down texture scales using this exponent", ImageConversionUtils.MinSizeExponent, ImageConversionUtils.MaxSizeExponent, "%d");
                DrawIntSlider(_picMip, "Scale down texture scales this many times", ImageConversionUtils.MinSizeExponent, ImageConversionUtils.MaxSizeExponent, "%d");

                DrawCheckbox(_overbright, "Enable overbright");
                DrawCheckbox(_fullbright, "Enable fullbright");

                ImGui.EndWindow();
            }
        }
    }
}
