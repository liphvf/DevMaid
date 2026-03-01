using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using NStack;
using Terminal.Gui;

namespace DevMaid.Tui;

public static class TuiApp
{
    private static Window? _mainWindow;
    private static ListView? _menuList;
    private static Label? _statusLabel;
    private static Label? _descriptionLabel;

    private static bool _isDarkTerminal;

    private static readonly List<MenuItem> MainMenuItems = new()
    {
        new MenuItem("Table Parser", "Parse and convert table data (CSV, Markdown, JSON)", RunTableParser),
        new MenuItem("File Utils", "File management utilities (search, organize, etc.)", RunFileUtils),
        new MenuItem("Claude Code", "Claude Code CLI integration", RunClaudeCode),
        new MenuItem("OpenCode", "OpenCode CLI integration", RunOpenCode),
        new MenuItem("Winget", "Backup and restore winget packages", RunWinget),
        new MenuItem("Exit", "Exit TUI mode", () => Application.RequestStop())
    };

    private static readonly List<MenuItem> TableParserItems = new()
    {
        new MenuItem("Parse CSV to Markdown", "Convert CSV file to Markdown table", () => RunCommand("devmaid table parse -i input.csv -o output.md")),
        new MenuItem("Parse CSV to JSON", "Convert CSV file to JSON format", () => RunCommand("devmaid table parse -i input.csv -o output.json --format json")),
        new MenuItem("Parse Markdown to CSV", "Convert Markdown table to CSV file", () => RunCommand("devmaid table parse -i input.md -o output.csv")),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static readonly List<MenuItem> FileUtilsItems = new()
    {
        new MenuItem("Search Files", "Search for files by name or pattern", () => RunCommand("devmaid file search")),
        new MenuItem("Organize by Extension", "Organize files by their extension", () => RunCommand("devmaid file organize")),
        new MenuItem("Find Duplicates", "Find duplicate files in directory", () => RunCommand("devmaid file duplicates")),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static readonly List<MenuItem> ClaudeCodeItems = new()
    {
        new MenuItem("Install Claude Code", "Install Claude Code CLI tool", () => RunCommand("devmaid claude install")),
        new MenuItem("Check Status", "Check Claude Code installation status", () => RunCommand("devmaid claude status")),
        new MenuItem("Configure", "Configure Claude Code settings", () => RunCommand("devmaid claude config")),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static readonly List<MenuItem> OpenCodeItems = new()
    {
        new MenuItem("Install OpenCode", "Install OpenCode CLI tool", () => RunCommand("devmaid opencode install")),
        new MenuItem("Check Status", "Check OpenCode installation status", () => RunCommand("devmaid opencode status")),
        new MenuItem("Configure", "Configure OpenCode settings", () => RunCommand("devmaid opencode config")),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static readonly List<MenuItem> WingetItems = new()
    {
        new MenuItem("Backup Packages", "Backup installed winget packages", () => RunCommand("devmaid winget backup")),
        new MenuItem("Restore Packages", "Restore winget packages from backup", () => RunCommand("devmaid winget restore")),
        new MenuItem("Back", "Return to main menu", () => { })
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
            Width = Dim.Fill() - 4,
            ColorScheme = colorScheme
        };

        var menuNames = GetMenuNames(MainMenuItems);
        _menuList = new ListView(menuNames)
        {
            X = 2,
            Y = 4,
            Width = 30,
            Height = Dim.Fill() - 8,
            AllowsMarking = false,
            ColorScheme = colorScheme
        };
        _menuList.SelectedItemChanged += OnMainMenuSelected;
        _menuList.OpenSelectedItem += OnMainMenuActivated;

        _descriptionLabel = new Label("")
        {
            X = 35,
            Y = 4,
            Width = Dim.Fill() - 37,
            Height = 5,
            TextAlignment = TextAlignment.Left,
            ColorScheme = colorScheme
        };

        var helpLabel = new Label("Keys: Enter Run  Esc Exit")
        {
            X = 2,
            Y = Pos.Bottom(_menuList) + 1,
            Width = Dim.Fill() - 4,
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

        UpdateMainDescription();
        Application.Run();
        Application.Shutdown();
    }

    private static string[] GetMenuNames(List<MenuItem> items)
    {
        var names = new string[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            names[i] = items[i].Name;
        }
        return names;
    }

    private static void OnMainMenuSelected(ListViewItemEventArgs args)
    {
        UpdateMainDescription();
    }

    private static void OnMainMenuActivated(object? args)
    {
        if (_menuList != null)
        {
            var selectedIndex = _menuList.SelectedItem;
            if (selectedIndex >= 0 && selectedIndex < MainMenuItems.Count)
            {
                MainMenuItems[selectedIndex].Action();
            }
        }
    }

    private static void UpdateMainDescription()
    {
        if (_menuList != null && _descriptionLabel != null)
        {
            var selectedIndex = _menuList.SelectedItem;
            if (selectedIndex >= 0 && selectedIndex < MainMenuItems.Count)
            {
                _descriptionLabel.Text = MainMenuItems[selectedIndex].Description;
            }
        }
    }

    private static void RunTableParser()
    {
        ShowSubMenu("Table Parser", TableParserItems);
    }

    private static void RunFileUtils()
    {
        ShowSubMenu("File Utils", FileUtilsItems);
    }

    private static void RunClaudeCode()
    {
        ShowSubMenu("Claude Code", ClaudeCodeItems);
    }

    private static void RunOpenCode()
    {
        ShowSubMenu("OpenCode", OpenCodeItems);
    }

    private static void RunWinget()
    {
        ShowSubMenu("Winget", WingetItems);
    }

    private static void ShowSubMenu(string title, List<MenuItem> items)
    {
        var colorScheme = GetColorScheme();

        var dialog = new Dialog(title)
        {
            Width = 50,
            Height = 15,
            ColorScheme = colorScheme
        };

        var menuNames = GetMenuNames(items);
        var listView = new ListView(menuNames)
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
            var selectedIndex = listView.SelectedItem;
            if (selectedIndex >= 0 && selectedIndex < items.Count)
            {
                var selectedItem = items[selectedIndex];
                if (selectedItem.Name == "Back")
                {
                    Application.RequestStop();
                }
                else
                {
                    Application.RequestStop();
                    selectedItem.Action();
                }
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

        var (dialog, outputText, exitCodeLabel) = ShowProgressDialog(command);

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

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

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                Application.MainLoop.Invoke(() =>
                {
                    outputText.Text = outputBuilder.ToString();
                    outputText.MoveEnd();
                });
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                Application.MainLoop.Invoke(() =>
                {
                    outputText.Text = outputBuilder.ToString() + "\n[ERROR]\n" + errorBuilder.ToString();
                    outputText.MoveEnd();
                });
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        Task.Run(() =>
        {
            process.WaitForExit();
            var exitCode = process.ExitCode;

            Application.MainLoop.Invoke(() =>
            {
                exitCodeLabel.Text = $"Exit Code: {exitCode} ({(exitCode == 0 ? "Success" : "Failed")})";
                exitCodeLabel.ColorScheme = new ColorScheme
                {
                    Normal = new Terminal.Gui.Attribute(exitCode == 0 ? Color.Green : Color.Red, GetBackgroundColor())
                };

                var closeButton = new Button("Close")
                {
                    X = Pos.Right(dialog) - 12,
                    Y = Pos.Bottom(dialog) - 3,
                    IsDefault = true
                };
                closeButton.Clicked += () => Application.RequestStop();
                dialog.Add(closeButton);
            });
        });

        Application.Run(dialog);
    }

    private static (Dialog dialog, TextView outputText, Label exitCodeLabel) ShowProgressDialog(string command)
    {
        var colorScheme = GetColorScheme();
        var bgColor = GetBackgroundColor();

        var dialog = new Dialog($"Running: {command}")
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
            ReadOnly = false,
            WordWrap = true,
            ColorScheme = colorScheme
        };

        var exitCodeLabel = new Label("Exit Code: Running...")
        {
            X = 1,
            Y = Pos.Bottom(outputText) + 1,
            ColorScheme = colorScheme
        };

        var cancelButton = new Button("Press Esc to cancel")
        {
            X = Pos.Right(dialog) - 25,
            Y = Pos.Bottom(dialog) - 3,
            IsDefault = false
        };

        dialog.Add(outputText, exitCodeLabel, cancelButton);

        Application.MainLoop.EventsPending(true);
        
        return (dialog, outputText, exitCodeLabel);
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
