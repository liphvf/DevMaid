using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NStack;
using Terminal.Gui;

namespace DevMaid.Commands;

public static class TuiApp
{
    private static Window? _mainWindow;
    private static ListView? _menuList;
    private static Label? _statusLabel;
    private static Label? _descriptionLabel;

    private static bool _isDarkTerminal;

    private static readonly List<(string Name, string Description, Action Action)> MenuItems = new()
    {
        ("Table Parser", "Parse and convert table data (CSV, Markdown, JSON)", RunTableParser),
        ("File Utils", "File management utilities (search, organize, etc.)", RunFileUtils),
        ("Claude Code", "Claude Code CLI integration", RunClaudeCode),
        ("OpenCode", "OpenCode CLI integration", RunOpenCode),
        ("Winget", "Backup and restore winget packages", RunWinget),
        ("Exit", "Exit TUI mode", () => Application.RequestStop())
    };

    private static void DetectTerminalTheme()
    {
        var term = Environment.GetEnvironmentVariable("TERM") ?? "";
        var colorTerm = Environment.GetEnvironmentVariable("COLORTERM") ?? "";
        var wtSession = Environment.GetEnvironmentVariable("WT_SESSION");
        
        if (term.Contains("light") || colorTerm.Contains("truecolor") || colorTerm.Contains("24bit"))
        {
            _isDarkTerminal = false;
        }
        else if (!string.IsNullOrEmpty(wtSession))
        {
            _isDarkTerminal = true;
        }
        else
        {
            _isDarkTerminal = true;
        }
    }

    private static ColorScheme GetColorScheme()
    {
        if (_isDarkTerminal)
        {
            return new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
                Focus = new Terminal.Gui.Attribute(Color.White, Color.DarkGray),
                HotNormal = new Terminal.Gui.Attribute(Color.BrightCyan, Color.Black),
                HotFocus = new Terminal.Gui.Attribute(Color.BrightCyan, Color.DarkGray)
            };
        }
        else
        {
            return new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.Black, Color.White),
                Focus = new Terminal.Gui.Attribute(Color.Black, Color.Gray),
                HotNormal = new Terminal.Gui.Attribute(Color.Blue, Color.White),
                HotFocus = new Terminal.Gui.Attribute(Color.Blue, Color.Gray)
            };
        }
    }

    private static Color GetTextColor(bool bright = false)
    {
        return _isDarkTerminal ? (bright ? Color.White : Color.Gray) : (bright ? Color.Black : Color.DarkGray);
    }

    private static Color GetBackgroundColor()
    {
        return _isDarkTerminal ? Color.Black : Color.White;
    }

    public static void Run()
    {
        Application.Init();

        DetectTerminalTheme();

        var colorScheme = GetColorScheme();
        var textColor = GetTextColor();
        var brightText = GetTextColor(true);
        var bgColor = GetBackgroundColor();

        _mainWindow = new Window("DevMaid - Terminal User Interface")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = colorScheme
        };

        var menuLabel = new Label("Select an option:")
        {
            X = 2,
            Y = 2,
            ColorScheme = colorScheme
        };

        _menuList = new ListView(MenuItems)
        {
            X = 2,
            Y = 4,
            Width = 25,
            Height = Dim.Fill() - 8,
            AllowsMarking = false,
            ColorScheme = colorScheme
        };
        _menuList.SelectedItemChanged += OnSelectedItemChanged;
        _menuList.OpenSelectedItem += OnMenuItemActivated;

        _descriptionLabel = new Label("")
        {
            X = 30,
            Y = 4,
            Width = Dim.Fill() - 32,
            Height = 5,
            TextAlignment = TextAlignment.Left,
            ColorScheme = colorScheme
        };

        var helpLabel = new Label("Keys: Enter Run  Esc Exit")
        {
            X = 2,
            Y = Pos.Bottom(_menuList) + 1,
            ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(textColor, bgColor) }
        };

        _statusLabel = new Label("Ready")
        {
            X = 2,
            Y = Pos.Bottom(_mainWindow) - 1,
            ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(brightText, bgColor) }
        };

        _mainWindow.Add(menuLabel, _menuList, _descriptionLabel, helpLabel, _statusLabel);
        Application.Top.Add(_mainWindow);

        UpdateDescription();
        Application.Run();
        Application.Shutdown();
    }

    private static void OnSelectedItemChanged(ListViewItemEventArgs args)
    {
        UpdateDescription();
    }

    private static void OnMenuItemActivated(object? args)
    {
        if (_menuList != null)
        {
            var selectedIndex = _menuList.SelectedItem;
            if (selectedIndex >= 0 && selectedIndex < MenuItems.Count)
            {
                MenuItems[selectedIndex].Action();
            }
        }
    }

    private static void UpdateDescription()
    {
        if (_menuList != null && _descriptionLabel != null)
        {
            var selectedIndex = _menuList.SelectedItem;
            if (selectedIndex >= 0 && selectedIndex < MenuItems.Count)
            {
                _descriptionLabel.Text = MenuItems[selectedIndex].Description;
            }
        }
    }

    private static void RunTableParser()
    {
        ShowSubMenu("Table Parser", new List<(string Name, Action)>
        {
            ("Parse CSV to Markdown", () => RunCommand("devmaid table parse -i input.csv -o output.md")),
            ("Parse CSV to JSON", () => RunCommand("devmaid table parse -i input.csv -o output.json --format json")),
            ("Parse Markdown to CSV", () => RunCommand("devmaid table parse -i input.md -o output.csv")),
            ("Back", () => { })
        });
    }

    private static void RunFileUtils()
    {
        ShowSubMenu("File Utils", new List<(string Name, Action)>
        {
            ("Search Files", () => RunCommand("devmaid file search")),
            ("Organize by Extension", () => RunCommand("devmaid file organize")),
            ("Find Duplicates", () => RunCommand("devmaid file duplicates")),
            ("Back", () => { })
        });
    }

    private static void RunClaudeCode()
    {
        ShowSubMenu("Claude Code", new List<(string Name, Action)>
        {
            ("Install Claude Code", () => RunCommand("devmaid claude install")),
            ("Check Status", () => RunCommand("devmaid claude status")),
            ("Configure", () => RunCommand("devmaid claude config")),
            ("Back", () => { })
        });
    }

    private static void RunOpenCode()
    {
        ShowSubMenu("OpenCode", new List<(string Name, Action)>
        {
            ("Install OpenCode", () => RunCommand("devmaid opencode install")),
            ("Check Status", () => RunCommand("devmaid opencode status")),
            ("Configure", () => RunCommand("devmaid opencode config")),
            ("Back", () => { })
        });
    }

    private static void RunWinget()
    {
        ShowSubMenu("Winget", new List<(string Name, Action)>
        {
            ("Backup Packages", () => RunCommand("devmaid winget backup")),
            ("Restore Packages", () => RunCommand("devmaid winget restore")),
            ("Back", () => { })
        });
    }

    private static void ShowSubMenu(string title, List<(string Name, Action Action)> items)
    {
        var colorScheme = GetColorScheme();

        var dialog = new Dialog(title)
        {
            Width = 50,
            Height = 15,
            ColorScheme = colorScheme
        };

        var listView = new ListView(items)
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 1,
            Height = Dim.Fill() - 3,
            AllowsMarking = false,
            ColorScheme = colorScheme
        };

        listView.OpenSelectedItem += (args) =>
        {
            var selected = items[listView.SelectedItem];
            if (selected.Name == "Back")
            {
                Application.RequestStop();
            }
            else
            {
                Application.RequestStop();
                selected.Action();
            }
        };

        Application.Current!.KeyPress += (e) =>
        {
            if (e.KeyEvent.Key == Key.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };

        dialog.Add(listView);
        Application.Run(dialog);
    }

    private static void RunCommand(string command)
    {
        _statusLabel!.Text = $"Running: {command}...";

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            ShowOutputDialog(command, output, error, process.ExitCode);
        }
        catch (Exception ex)
        {
            _statusLabel!.Text = $"Error: {ex.Message}";
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private static void ShowOutputDialog(string command, string output, string error, int exitCode)
    {
        var colorScheme = GetColorScheme();
        var bgColor = GetBackgroundColor();

        var dialog = new Dialog($"Output: {command}")
        {
            Width = Dim.Percent(80),
            Height = Dim.Percent(60),
            ColorScheme = colorScheme
        };

        var outputText = new TextView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 6,
            Text = string.IsNullOrEmpty(output) ? "(no output)" : output,
            ReadOnly = true,
            WordWrap = true,
            ColorScheme = colorScheme
        };

        var exitCodeText = exitCode == 0 ? "Success" : "Failed";
        var exitCodeLabel = new Label($"Exit Code: {exitCode} ({exitCodeText})")
        {
            X = 1,
            Y = Pos.Bottom(outputText) + 1,
            ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(exitCode == 0 ? Color.Green : Color.Red, bgColor) }
        };

        dialog.Add(outputText);

        if (!string.IsNullOrEmpty(error))
        {
            var errorText = error.Length > 65 ? error.Substring(0, 62) + "..." : error;
            var errorLabel = new Label($"Error: {errorText}")
            {
                X = 1,
                Y = Pos.Bottom(exitCodeLabel),
                ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(Color.Red, bgColor) }
            };
            dialog.Add(errorLabel);
            dialog.Add(exitCodeLabel);
        }
        else
        {
            dialog.Add(exitCodeLabel);
        }

        var closeButton = new Button("Close")
        {
            X = Pos.Right(dialog) - 12,
            Y = Pos.Bottom(dialog) - 3,
            IsDefault = true
        };
        closeButton.Clicked += () => Application.RequestStop();

        dialog.Add(closeButton);
        Application.Run(dialog);
    }
}
