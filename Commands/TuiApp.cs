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

    private static readonly List<(string Name, string Description, Action Action)> MenuItems = new()
    {
        ("Table Parser", "Parse and convert table data (CSV, Markdown, JSON)", RunTableParser),
        ("File Utils", "File management utilities (search, organize, etc.)", RunFileUtils),
        ("Claude Code", "Claude Code CLI integration", RunClaudeCode),
        ("OpenCode", "OpenCode CLI integration", RunOpenCode),
        ("Winget", "Backup and restore winget packages", RunWinget),
        ("Exit", "Exit TUI mode", () => Application.RequestStop())
    };

    public static void Run()
    {
        Application.Init();

        _mainWindow = new Window(new Rect(0, 0, 80, 25), "DevMaid - Terminal User Interface");

        var menuLabel = new Label(2, 2, "Select an option:");

        _menuList = new ListView(new Rect(2, 4, 25, 15), MenuItems)
        {
            AllowsMarking = false
        };
        _menuList.SelectedItemChanged += OnSelectedItemChanged;
        _menuList.OpenSelectedItem += OnMenuItemActivated;

        _descriptionLabel = new Label(new Rect(30, 4, 48, 5), "")
        {
            TextAlignment = TextAlignment.Left
        };

        var helpLabel = new Label(2, 20, "Keys: Enter Run  Esc Exit")
        {
            ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black) }
        };

        _statusLabel = new Label(2, 23, "Ready")
        {
            ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(Color.Green, Color.Black) }
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
        var dialog = new Dialog(title)
        {
            Width = 50,
            Height = 15
        };

        var listView = new ListView(items)
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 1,
            Height = Dim.Fill() - 3,
            AllowsMarking = false
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
        var dialog = new Dialog($"Output: {command}", 70, 20);

        var outputText = new TextView(new Rect(1, 1, 68, 12))
        {
            Text = string.IsNullOrEmpty(output) ? "(no output)" : output,
            ReadOnly = true,
            WordWrap = true
        };

        var exitCodeText = exitCode == 0 ? "Success" : "Failed";
        var exitCodeLabel = new Label(1, 14, $"Exit Code: {exitCode} ({exitCodeText})")
        {
            ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(exitCode == 0 ? Color.Green : Color.Red, Color.Black) }
        };

        dialog.Add(outputText);

        if (!string.IsNullOrEmpty(error))
        {
            var errorText = error.Length > 65 ? error.Substring(0, 62) + "..." : error;
            var errorLabel = new Label(1, 15, $"Error: {errorText}")
            {
                ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(Color.Red, Color.Black) }
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
