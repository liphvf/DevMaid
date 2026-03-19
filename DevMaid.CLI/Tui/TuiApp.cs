using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NStack;
using Terminal.Gui;

namespace DevMaid.CLI.Tui;

internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public short wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    public const int STD_OUTPUT_HANDLE = -11;
}

public static class TuiApp
{
    private static Window? _mainWindow;
    private static ListView? _menuList;
    private static Label? _statusLabel;
    private static Label? _descriptionLabel;

    private static bool _isDarkTerminal;

    private static readonly List<MenuItem> MainMenuItems = new()
    {
        new MenuItem("Table Parser", "Convert database table to C# properties class", RunTableParser),
        new MenuItem("File Utils", "File management utilities (search, organize, etc.)", RunFileUtils),
        new MenuItem("Database", "PostgreSQL backup and restore utilities", RunDatabase),
        new MenuItem("Claude Code", "Claude Code CLI integration", RunClaudeCode),
        new MenuItem("OpenCode", "OpenCode CLI integration", RunOpenCode),
        new MenuItem("Winget", "Backup and restore winget packages", RunWinget),
        new MenuItem("Exit", "Exit TUI mode", () => Application.RequestStop())
    };

    private static readonly List<MenuItem> TableParserItems = new()
    {
        new MenuItem("Convert Table to C#", "Convert database table to C# properties class", RunTableParserDialog),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static readonly List<MenuItem> FileUtilsItems = new()
    {
        new MenuItem("Combine Files", "Combine files in a directory into a single file", RunFileCombineDialog),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static readonly List<MenuItem> DatabaseItems = new()
    {
        new MenuItem("Backup", "Create a backup of a PostgreSQL database", RunDatabaseBackupDialog),
        new MenuItem("Restore", "Restore a PostgreSQL database from backup", RunDatabaseRestoreDialog),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static readonly List<MenuItem> ClaudeCodeItems = new()
    {
        new MenuItem("Install Claude Code", "Install Claude Code CLI tool", () => RunCommand("devmaid claude install")),
        new MenuItem("Settings", "Configure Claude Code settings", RunClaudeCodeSettings),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static readonly List<MenuItem> ClaudeCodeSettingsItems = new()
    {
        new MenuItem("MCP Database", "Configure MCP database integration", () => RunCommand("devmaid claude settings mcp-database")),
        new MenuItem("Windows Environment", "Configure Windows environment and CLAUDE.md", () => RunCommand("devmaid claude settings win-env")),
        new MenuItem("Back", "Return to Claude Code menu", () => { })
    };

    private static readonly List<MenuItem> OpenCodeItems = new()
    {
        new MenuItem("Settings", "Configure OpenCode settings", RunOpenCodeSettings),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static readonly List<MenuItem> OpenCodeSettingsItems = new()
    {
        new MenuItem("MCP Database", "Configure MCP database integration", () => RunCommand("devmaid opencode settings mcp-database")),
        new MenuItem("Back", "Return to OpenCode menu", () => { })
    };

    private static readonly List<MenuItem> WingetItems = new()
    {
        new MenuItem("Backup Packages", "Backup installed winget packages", RunWingetBackupDialog),
        new MenuItem("Restore Packages", "Restore winget packages from backup", RunWingetRestoreDialog),
        new MenuItem("Back", "Return to main menu", () => { })
    };

    private static void DetectTerminalTheme()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use Windows Console API to get actual console colors
            try
            {
                IntPtr hConsole = NativeMethods.GetStdHandle(NativeMethods.STD_OUTPUT_HANDLE);
                if (hConsole != IntPtr.Zero && hConsole != new IntPtr(-1))
                {
                    if (NativeMethods.GetConsoleScreenBufferInfo(hConsole, out NativeMethods.CONSOLE_SCREEN_BUFFER_INFO csbi))
                    {
                        // Extract background color from wAttributes
                        // The background color is in the upper 4 bits of wAttributes
                        short bgColor = (short)((csbi.wAttributes >> 4) & 0x0F);
                        
                        // Check if background is dark (0-7) or light (8-15)
                        // Windows console colors: 0=Black, 1=DarkBlue, 2=DarkGreen, 3=DarkCyan, 
                        // 4=DarkRed, 5=DarkMagenta, 6=DarkYellow, 7=DarkGray,
                        // 8=Black (intense), 9=Blue, 10=Green, 11=Cyan,
                        // 12=Red, 13=Magenta, 14=Yellow, 15=White
                        _isDarkTerminal = bgColor < 8;
                        return;
                    }
                }
            }
            catch
            {
                // Fallback if API call fails
            }
        }

        // Fallback for non-Windows or if detection fails
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
                Normal = new Terminal.Gui.Attribute(Color.DarkGray, Color.White),
                Focus = new Terminal.Gui.Attribute(Color.DarkGray, Color.Gray),
                HotNormal = new Terminal.Gui.Attribute(Color.Blue, Color.White),
                HotFocus = new Terminal.Gui.Attribute(Color.Blue, Color.Gray)
            };
        }
    }

    private static Color GetTextColor(bool bright = false)
    {
        return _isDarkTerminal ? (bright ? Color.White : Color.Gray) : (bright ? Color.DarkGray : Color.Gray);
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

    private static void RunTableParserDialog()
    {
        var colorScheme = GetColorScheme();

        var dialog = new Dialog("Table Parser - Database to C#")
        {
            Width = Dim.Percent(60),
            Height = Dim.Percent(50),
            ColorScheme = colorScheme
        };

        var yPos = 1;

        var dbLabel = new Label("Database Name (*):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var dbField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var tableLabel = new Label("Table Name (*):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var tableField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var hostLabel = new Label("Host (default: localhost):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var hostField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var userLabel = new Label("User (default: postgres):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var userField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var outputLabel = new Label("Output (default: ./Table.class):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var outputField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 3;

        var convertButton = new Button("Convert")
        {
            X = 2,
            Y = yPos,
            IsDefault = true,
            ColorScheme = colorScheme
        };

        var cancelButton = new Button("Cancel")
        {
            X = Pos.Right(convertButton) + 2,
            Y = yPos,
            ColorScheme = colorScheme
        };

        convertButton.Clicked += () =>
        {
            if (string.IsNullOrWhiteSpace(dbField.Text.ToString()))
            {
                MessageBox.ErrorQuery("Error", "Database name is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(tableField.Text.ToString()))
            {
                MessageBox.ErrorQuery("Error", "Table name is required.", "OK");
                return;
            }

            var db = dbField.Text.ToString();
            var table = tableField.Text.ToString();
            var host = string.IsNullOrWhiteSpace(hostField.Text.ToString()) ? "localhost" : hostField.Text.ToString();
            var user = string.IsNullOrWhiteSpace(userField.Text.ToString()) ? "postgres" : userField.Text.ToString();
            var output = string.IsNullOrWhiteSpace(outputField.Text.ToString()) ? "./Table.class" : outputField.Text.ToString();

            // Validate database name
            if (!DevMaid.CLI.SecurityUtils.IsValidPostgreSQLIdentifier(db!))
            {
                MessageBox.ErrorQuery("Error", "Invalid database name. Use only letters, numbers, and underscores.", "OK");
                return;
            }

            // Validate table name
            if (!DevMaid.CLI.SecurityUtils.IsValidPostgreSQLIdentifier(table!))
            {
                MessageBox.ErrorQuery("Error", "Invalid table name. Use only letters, numbers, and underscores.", "OK");
                return;
            }

            // Validate host
            if (!DevMaid.CLI.SecurityUtils.IsValidHost(host!))
            {
                MessageBox.ErrorQuery("Error", "Invalid host.", "OK");
                return;
            }

            // Validate username
            if (!DevMaid.CLI.SecurityUtils.IsValidUsername(user!))
            {
                MessageBox.ErrorQuery("Error", "Invalid username. Use only letters, numbers, underscores, hyphens, and dots.", "OK");
                return;
            }

            // Validate output path
            if (!DevMaid.CLI.SecurityUtils.IsValidPath(output!))
            {
                MessageBox.ErrorQuery("Error", "Invalid output path.", "OK");
                return;
            }

            Application.RequestStop();

            var command = $"devmaid table-parser -d \"{db}\" -t \"{table}\" -H \"{host}\" -u \"{user}\" -o \"{output}\"";
            RunCommand(command);
        };

        cancelButton.Clicked += () => Application.RequestStop();

        dialog.Add(dbLabel, dbField, tableLabel, tableField, hostLabel, hostField, userLabel, userField, outputLabel, outputField, convertButton, cancelButton);

        Application.Run(dialog);
    }

    private static void RunDatabaseBackupDialog()
    {
        var colorScheme = GetColorScheme();

        var dialog = new Dialog("Database Backup")
        {
            Width = Dim.Percent(60),
            Height = Dim.Percent(50),
            ColorScheme = colorScheme
        };

        var yPos = 1;

        var allCheckbox = new CheckBox("Backup all databases")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var dbLabel = new Label("Database Name (*):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var dbField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var hostLabel = new Label("Host (default: localhost):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var hostField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var portLabel = new Label("Port (default: 5432):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var portField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var userLabel = new Label("Username:")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var userField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var outputLabel = new Label("Output (default: current dir):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var outputField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 3;

        var backupButton = new Button("Backup")
        {
            X = 2,
            Y = yPos,
            IsDefault = true,
            ColorScheme = colorScheme
        };

        var cancelButton = new Button("Cancel")
        {
            X = Pos.Right(backupButton) + 2,
            Y = yPos,
            ColorScheme = colorScheme
        };

        backupButton.Clicked += () =>
        {
            var allDatabases = allCheckbox.Checked;

            if (!allDatabases && string.IsNullOrWhiteSpace(dbField.Text.ToString()))
            {
                MessageBox.ErrorQuery("Error", "Database name is required when not backing up all databases.", "OK");
                return;
            }

            var db = dbField.Text.ToString();
            var host = string.IsNullOrWhiteSpace(hostField.Text.ToString()) ? "localhost" : hostField.Text.ToString();
            var port = string.IsNullOrWhiteSpace(portField.Text.ToString()) ? "5432" : portField.Text.ToString();
            var user = string.IsNullOrWhiteSpace(userField.Text.ToString()) ? "" : userField.Text.ToString();
            var output = string.IsNullOrWhiteSpace(outputField.Text.ToString()) ? "" : outputField.Text.ToString();

            // Validate database name
            if (!allDatabases && !string.IsNullOrWhiteSpace(db) && !DevMaid.CLI.SecurityUtils.IsValidPostgreSQLIdentifier(db))
            {
                MessageBox.ErrorQuery("Error", "Invalid database name. Use only letters, numbers, and underscores.", "OK");
                return;
            }

            // Validate host
            if (!string.IsNullOrWhiteSpace(host) && !DevMaid.CLI.SecurityUtils.IsValidHost(host))
            {
                MessageBox.ErrorQuery("Error", "Invalid host.", "OK");
                return;
            }

            // Validate port
            if (!string.IsNullOrWhiteSpace(port) && !DevMaid.CLI.SecurityUtils.IsValidPort(port))
            {
                MessageBox.ErrorQuery("Error", "Invalid port. Must be between 1 and 65535.", "OK");
                return;
            }

            // Validate username
            if (!string.IsNullOrWhiteSpace(user) && !DevMaid.CLI.SecurityUtils.IsValidUsername(user))
            {
                MessageBox.ErrorQuery("Error", "Invalid username. Use only letters, numbers, underscores, hyphens, and dots.", "OK");
                return;
            }

            // Validate output path
            if (!string.IsNullOrWhiteSpace(output) && !DevMaid.CLI.SecurityUtils.IsValidPath(output))
            {
                MessageBox.ErrorQuery("Error", "Invalid output path.", "OK");
                return;
            }

            Application.RequestStop();

            var command = "devmaid database backup";
            if (allDatabases)
            {
                command += " --all";
            }
            else
            {
                command += $" \"{db}\"";
            }

            if (!string.IsNullOrEmpty(host)) { command += $" --host \"{host}\""; }
            if (!string.IsNullOrEmpty(port)) { command += $" --port \"{port}\""; }
            if (!string.IsNullOrEmpty(user)) { command += $" --username \"{user}\""; }
            if (!string.IsNullOrEmpty(output)) { command += $" --output \"{output}\""; }

            RunCommand(command);
        };

        cancelButton.Clicked += () => Application.RequestStop();

        dialog.Add(allCheckbox, dbLabel, dbField, hostLabel, hostField, portLabel, portField, userLabel, userField, outputLabel, outputField, backupButton, cancelButton);

        Application.Run(dialog);
    }

    private static void RunDatabaseRestoreDialog()
    {
        var colorScheme = GetColorScheme();

        var dialog = new Dialog("Database Restore")
        {
            Width = Dim.Percent(60),
            Height = Dim.Percent(50),
            ColorScheme = colorScheme
        };

        var yPos = 1;

        var allCheckbox = new CheckBox("Restore all databases")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var dbLabel = new Label("Database Name (*):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var dbField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var fileLabel = new Label("Dump File:")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var fileField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var hostLabel = new Label("Host (default: localhost):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var hostField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var portLabel = new Label("Port (default: 5432):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var portField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var userLabel = new Label("Username:")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var userField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 3;

        var restoreButton = new Button("Restore")
        {
            X = 2,
            Y = yPos,
            IsDefault = true,
            ColorScheme = colorScheme
        };

        var cancelButton = new Button("Cancel")
        {
            X = Pos.Right(restoreButton) + 2,
            Y = yPos,
            ColorScheme = colorScheme
        };

        restoreButton.Clicked += () =>
        {
            var allDatabases = allCheckbox.Checked;

            if (!allDatabases && string.IsNullOrWhiteSpace(dbField.Text.ToString()))
            {
                MessageBox.ErrorQuery("Error", "Database name is required when not restoring all databases.", "OK");
                return;
            }

            var db = dbField.Text.ToString();
            var file = string.IsNullOrWhiteSpace(fileField.Text.ToString()) ? "" : fileField.Text.ToString();
            var host = string.IsNullOrWhiteSpace(hostField.Text.ToString()) ? "localhost" : hostField.Text.ToString();
            var port = string.IsNullOrWhiteSpace(portField.Text.ToString()) ? "5432" : portField.Text.ToString();
            var user = string.IsNullOrWhiteSpace(userField.Text.ToString()) ? "" : userField.Text.ToString();

            // Validate database name
            if (!allDatabases && !string.IsNullOrWhiteSpace(db) && !DevMaid.CLI.SecurityUtils.IsValidPostgreSQLIdentifier(db))
            {
                MessageBox.ErrorQuery("Error", "Invalid database name. Use only letters, numbers, and underscores.", "OK");
                return;
            }

            // Validate host
            if (!string.IsNullOrWhiteSpace(host) && !DevMaid.CLI.SecurityUtils.IsValidHost(host))
            {
                MessageBox.ErrorQuery("Error", "Invalid host.", "OK");
                return;
            }

            // Validate port
            if (!string.IsNullOrWhiteSpace(port) && !DevMaid.CLI.SecurityUtils.IsValidPort(port))
            {
                MessageBox.ErrorQuery("Error", "Invalid port. Must be between 1 and 65535.", "OK");
                return;
            }

            // Validate username
            if (!string.IsNullOrWhiteSpace(user) && !DevMaid.CLI.SecurityUtils.IsValidUsername(user))
            {
                MessageBox.ErrorQuery("Error", "Invalid username. Use only letters, numbers, underscores, hyphens, and dots.", "OK");
                return;
            }

            // Validate file/directory path
            if (!string.IsNullOrWhiteSpace(file) && !DevMaid.CLI.SecurityUtils.IsValidPath(file))
            {
                MessageBox.ErrorQuery("Error", "Invalid file path.", "OK");
                return;
            }

            Application.RequestStop();

            var command = "devmaid database restore";
            if (allDatabases)
            {
                command += " --all";
                if (!string.IsNullOrEmpty(file)) { command += $" --directory \"{file}\""; }
            }
            else
            {
                command += $" \"{db}\"";
                if (!string.IsNullOrEmpty(file)) { command += $" \"{file}\""; }
            }

            if (!string.IsNullOrEmpty(host)) { command += $" --host \"{host}\""; }
            if (!string.IsNullOrEmpty(port)) { command += $" --port \"{port}\""; }
            if (!string.IsNullOrEmpty(user)) { command += $" --username \"{user}\""; }

            RunCommand(command);
        };

        cancelButton.Clicked += () => Application.RequestStop();

        dialog.Add(allCheckbox, dbLabel, dbField, fileLabel, fileField, hostLabel, hostField, portLabel, portField, userLabel, userField, restoreButton, cancelButton);

        Application.Run(dialog);
    }

    private static void RunWingetBackupDialog()
    {
        var colorScheme = GetColorScheme();

        var dialog = new Dialog("Winget Backup")
        {
            Width = Dim.Percent(50),
            Height = 15,
            ColorScheme = colorScheme
        };

        var yPos = 1;

        var outputLabel = new Label("Output Directory (default: current):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var outputField = new TextField("")
        {
            X = 35,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 3;

        var backupButton = new Button("Backup")
        {
            X = 2,
            Y = yPos,
            IsDefault = true,
            ColorScheme = colorScheme
        };

        var cancelButton = new Button("Cancel")
        {
            X = Pos.Right(backupButton) + 2,
            Y = yPos,
            ColorScheme = colorScheme
        };

        backupButton.Clicked += () =>
        {
            var output = string.IsNullOrWhiteSpace(outputField.Text.ToString()) ? "" : outputField.Text.ToString();

            Application.RequestStop();

            var command = "devmaid winget backup";
            if (!string.IsNullOrEmpty(output)) { command += $" --output \"{output}\""; }

            RunCommand(command);
        };

        cancelButton.Clicked += () => Application.RequestStop();

        dialog.Add(outputLabel, outputField, backupButton, cancelButton);

        Application.Run(dialog);
    }

    private static void RunWingetRestoreDialog()
    {
        var colorScheme = GetColorScheme();

        var dialog = new Dialog("Winget Restore")
        {
            Width = Dim.Percent(50),
            Height = 15,
            ColorScheme = colorScheme
        };

        var yPos = 1;

        var inputLabel = new Label("Input File (default: backup-winget.json):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var inputField = new TextField("")
        {
            X = 40,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 3;

        var restoreButton = new Button("Restore")
        {
            X = 2,
            Y = yPos,
            IsDefault = true,
            ColorScheme = colorScheme
        };

        var cancelButton = new Button("Cancel")
        {
            X = Pos.Right(restoreButton) + 2,
            Y = yPos,
            ColorScheme = colorScheme
        };

        restoreButton.Clicked += () =>
        {
            var input = string.IsNullOrWhiteSpace(inputField.Text.ToString()) ? "" : inputField.Text.ToString();

            Application.RequestStop();

            var command = "devmaid winget restore";
            if (!string.IsNullOrEmpty(input)) { command += $" --input \"{input}\""; }

            RunCommand(command);
        };

        cancelButton.Clicked += () => Application.RequestStop();

        dialog.Add(inputLabel, inputField, restoreButton, cancelButton);

        Application.Run(dialog);
    }

    private static void RunFileCombineDialog()
    {
        var colorScheme = GetColorScheme();

        var dialog = new Dialog("File Combine")
        {
            Width = Dim.Percent(60),
            Height = 15,
            ColorScheme = colorScheme
        };

        var yPos = 1;

        var inputLabel = new Label("Input Pattern (*):")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var inputField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 2;

        var outputLabel = new Label("Output Path:")
        {
            X = 2,
            Y = yPos,
            ColorScheme = colorScheme
        };
        var outputField = new TextField("")
        {
            X = 25,
            Y = yPos,
            Width = Dim.Fill() - 3,
            ColorScheme = colorScheme
        };

        yPos += 3;

        var combineButton = new Button("Combine")
        {
            X = 2,
            Y = yPos,
            IsDefault = true,
            ColorScheme = colorScheme
        };

        var cancelButton = new Button("Cancel")
        {
            X = Pos.Right(combineButton) + 2,
            Y = yPos,
            ColorScheme = colorScheme
        };

        combineButton.Clicked += () =>
        {
            if (string.IsNullOrWhiteSpace(inputField.Text.ToString()))
            {
                MessageBox.ErrorQuery("Error", "Input pattern is required.", "OK");
                return;
            }

            var input = inputField.Text.ToString();
            var output = string.IsNullOrWhiteSpace(outputField.Text.ToString()) ? "" : outputField.Text.ToString();

            // Validate output path
            if (!string.IsNullOrWhiteSpace(output) && !DevMaid.CLI.SecurityUtils.IsValidPath(output))
            {
                MessageBox.ErrorQuery("Error", "Invalid output path.", "OK");
                return;
            }

            Application.RequestStop();

            var command = $"devmaid file combine --input \"{input}\"";
            if (!string.IsNullOrEmpty(output)) { command += $" --output \"{output}\""; }

            RunCommand(command);
        };

        cancelButton.Clicked += () => Application.RequestStop();

        dialog.Add(inputLabel, inputField, outputLabel, outputField, combineButton, cancelButton);

        Application.Run(dialog);
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

    private static void RunDatabase()
    {
        ShowSubMenu("Database", DatabaseItems);
    }

    private static void RunClaudeCodeSettings()
    {
        ShowSubMenu("Claude Code Settings", ClaudeCodeSettingsItems);
    }

    private static void RunOpenCodeSettings()
    {
        ShowSubMenu("OpenCode Settings", OpenCodeSettingsItems);
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
        // Validate command to prevent command injection
        if (!IsValidDevMaidCommand(command))
        {
            MessageBox.ErrorQuery("Security Error", "Invalid command. Only DevMaid commands are allowed.", "OK");
            return;
        }

        _statusLabel!.Text = $"Running: {command}...";

        var colorScheme = GetColorScheme();
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
                    IsDefault = true,
                    ColorScheme = colorScheme
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
            IsDefault = false,
            ColorScheme = colorScheme
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
            IsDefault = true,
            ColorScheme = colorScheme
        };
        closeButton.Clicked += () => Application.RequestStop();

        dialog.Add(closeButton);
        Application.Run(dialog);
    }

    private static bool IsValidDevMaidCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        // Allow only DevMaid commands starting with "devmaid "
        if (!command.TrimStart().StartsWith("devmaid ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Define allowed subcommands
        var allowedSubcommands = new[]
        {
            "devmaid table-parser",
            "devmaid file",
            "devmaid database",
            "devmaid claude",
            "devmaid opencode",
            "devmaid winget"
        };

        // Check if command starts with any allowed subcommand
        var trimmedCommand = command.TrimStart();
        foreach (var subcommand in allowedSubcommands)
        {
            if (trimmedCommand.StartsWith(subcommand, StringComparison.OrdinalIgnoreCase))
            {
                // Additional validation for dangerous characters
                if (ContainsDangerousCharacters(trimmedCommand))
                {
                    return false;
                }
                return true;
            }
        }

        return false;
    }

    private static bool ContainsDangerousCharacters(string command)
    {
        // Check for command injection characters
        var dangerousChars = new[] { '&', '|', ';', '$', '`', '\\', '<', '>', '\n', '\r' };
        
        foreach (var dangerousChar in dangerousChars)
        {
            if (command.Contains(dangerousChar))
            {
                return true;
            }
        }

        return false;
    }
}
