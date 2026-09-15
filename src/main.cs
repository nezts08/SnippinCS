using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnippinCS
{    public class Config
    {
        [JsonPropertyName("trigger_key")]
        public ushort TriggerKey { get; set; } = 9;

        [JsonPropertyName("trigger_key_name")]
        public string TriggerKeyName { get; set; } = "TAB";

        [JsonPropertyName("commands")]
        public Dictionary<string, string> Commands { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private const string ConfigFile = "command.json";
        private static Config config;
        private static string buffer = "";

        public static Dictionary<string, ushort> KeyMap = new Dictionary<string, ushort>
        {
            { "TAB", 9 },
            { "F1", 112 },
            { "F2", 113 },
            { "F3", 114 },
            { "F4", 115 },
            { "F5", 116 },
            { "F6", 117 },
            { "F7", 118 },
            { "F8", 119 },
            { "F9", 120 },
            { "F10", 121 },
        };

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private ListBox listCommands;
        private TextBox txtKey;
        private TextBox txtValue;
        private Label rightTitle;
        private Button btnSave;
        private Button btnDelete;

        public MainForm()
        {
            LoadConfig();
            InitializeUI();

            // Iniciar motor de teclado
            _hookID = SetHook(_proc);
            this.FormClosing += (s, e) => UnhookWindowsHookEx(_hookID);
        }

        private void InitializeUI()
        {
            Color bgDark = Color.FromArgb(30, 30, 30);
            Color panelDark = Color.FromArgb(37, 37, 38);
            Color inputDark = Color.FromArgb(45, 45, 48);
            Color textLight = Color.FromArgb(241, 241, 241);
            Color accentBlue = Color.FromArgb(0, 122, 204);
            Color dangerRed = Color.FromArgb(197, 34, 31);

            this.Text = "SnippinC#";
            this.Size = new Size(1200, 700);
            this.MinimumSize = new Size(900, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgDark;
            this.ForeColor = textLight;
            this.Font = new Font("Segoe UI", 10F);

            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = bgDark,
            };

            Panel leftHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(10, 8, 10, 8),
                BackColor = bgDark,
            };

            Label lblTitle = new Label
            {
                Text = "COMANDOS",
                Font = new Font(this.Font, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
            };

            Button btnSettings = new Button
            {
                Text = "⚙",
                Dock = DockStyle.Right,
                Width = 35,
                FlatStyle = FlatStyle.Flat,
                ForeColor = textLight,
                BackColor = inputDark,
                Cursor = Cursors.Hand,
            };

            btnSettings.FlatAppearance.BorderSize = 0;

            Button btnAdd = new Button
            {
                Text = "+",
                Dock = DockStyle.Right,
                Width = 35,
                FlatStyle = FlatStyle.Flat,
                ForeColor = textLight,
                BackColor = inputDark,
                Cursor = Cursors.Hand,
            };

            btnAdd.FlatAppearance.BorderSize = 0;

            leftHeader.Controls.Add(lblTitle);
            leftHeader.Controls.Add(btnAdd);
            leftHeader.Controls.Add(btnSettings);

            listCommands = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = bgDark,
                ForeColor = textLight,
                BorderStyle = BorderStyle.None,
                ItemHeight = 25,
                Font = new Font("Consolas", 11F),
                IntegralHeight = false,
            };

            UpdateCommandList();

            leftPanel.Controls.Add(listCommands);
            leftPanel.Controls.Add(leftHeader);

            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgDark,
                Padding = new Padding(25),
            };

            rightTitle = new Label
            {
                Text = "Nuevo Snippet",
                Dock = DockStyle.Top,
                Height = 45,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
            };

            Label lblCommand = new Label
            {
                Text = "Comando:",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = Color.Gray,
            };

            txtKey = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = inputDark,
                ForeColor = textLight,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 11F),
                PlaceholderText = "Ej: html5",
            };

            Panel spacer1 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 20,
                BackColor = bgDark,
            };

            Label lblExpanded = new Label
            {
                Text = "Texto expandido:",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = Color.Gray,
            };

            Panel textContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 20),
                BackColor = bgDark,
            };

            txtValue = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = inputDark,
                ForeColor = textLight,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 11F),
            };

            textContainer.Controls.Add(txtValue);

            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = bgDark,
            };

            btnSave = new Button
            {
                Text = "Guardar",
                Dock = DockStyle.Left,
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                BackColor = accentBlue,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font(this.Font, FontStyle.Bold),
            };

            btnSave.FlatAppearance.BorderSize = 0;

            btnDelete = new Button
            {
                Text = "Eliminar",
                Dock = DockStyle.Left,
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                BackColor = dangerRed,
                ForeColor = Color.White,
                Visible = false,
                Cursor = Cursors.Hand,
                Font = new Font(this.Font, FontStyle.Bold),
                Margin = new Padding(10, 0, 0, 0),
            };

            btnDelete.FlatAppearance.BorderSize = 0;

            buttonPanel.Controls.Add(btnDelete);
            buttonPanel.Controls.Add(btnSave);

            rightPanel.Controls.Add(textContainer);
            rightPanel.Controls.Add(lblExpanded);
            rightPanel.Controls.Add(spacer1);
            rightPanel.Controls.Add(txtKey);
            rightPanel.Controls.Add(lblCommand);
            rightPanel.Controls.Add(rightTitle);
            rightPanel.Controls.Add(buttonPanel);

            Label footerNote = new Label
            {
                Text =
                    "*NOTA: recuerda que al momento de un espaciado o enter se borra la captura del texto anterior.",
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font, FontStyle.Italic),
                Height = 35,
                ForeColor = Color.DarkGray,
                BackColor = panelDark,
            };

            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);
            this.Controls.Add(footerNote);

            listCommands.SelectedIndexChanged += (s, e) =>
            {
                if (listCommands.SelectedItem == null)
                    return;

                string key = listCommands.SelectedItem.ToString()!;

                txtKey.Text = key;
                txtKey.Enabled = false;
                txtKey.BackColor = panelDark;

                txtValue.Text = config.Commands[key];

                btnDelete.Visible = true;
                rightTitle.Text = "Editando Snippet";
            };

            btnAdd.Click += (s, e) =>
            {
                listCommands.ClearSelected();

                txtKey.Enabled = true;
                txtKey.BackColor = inputDark;

                txtKey.Text = "";
                txtValue.Text = "";

                btnDelete.Visible = false;
                rightTitle.Text = "Nuevo Snippet";
            };

            btnSave.Click += (s, e) =>
            {
                if (
                    !string.IsNullOrWhiteSpace(txtKey.Text)
                    && !string.IsNullOrWhiteSpace(txtValue.Text)
                )
                {
                    config.Commands[txtKey.Text.ToLower()] = txtValue.Text;

                    SaveConfig();
                    UpdateCommandList();

                    btnAdd.PerformClick();
                }
            };

            btnDelete.Click += (s, e) =>
            {
                string key = txtKey.Text.ToLower();

                if (config.Commands.ContainsKey(key))
                {
                    config.Commands.Remove(key);

                    SaveConfig();
                    UpdateCommandList();

                    btnAdd.PerformClick();
                }
            };

            btnSettings.Click += (s, e) => ShowSettingsModal();
        }

        private void ShowSettingsModal()
        {
            Color bgDark = Color.FromArgb(30, 30, 30);
            Color inputDark = Color.FromArgb(45, 45, 48);
            Color textLight = Color.FromArgb(241, 241, 241);
            Color accentBlue = Color.FromArgb(0, 122, 204);

            Form modal = new Form
            {
                Text = "Configuración",
                Size = new Size(320, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = bgDark,
                ForeColor = textLight,
            };

            Label lblInfo = new Label
            {
                Text = "Selecciona la tecla de activación:",
                Location = new Point(25, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
            };

            ComboBox combo = new ComboBox
            {
                Location = new Point(25, 60),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = inputDark,
                ForeColor = textLight,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F),
            };
            combo.Items.AddRange(KeyMap.Keys.ToArray());
            combo.SelectedItem = config.TriggerKeyName;

            Button btnClose = new Button
            {
                Text = "Guardar cambios",
                Location = new Point(25, 115),
                Width = 250,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = accentBlue,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            };
            btnClose.FlatAppearance.BorderSize = 0;

            btnClose.Click += (s, e) =>
            {
                if (combo.SelectedItem != null)
                {
                    config.TriggerKeyName = combo.SelectedItem.ToString()!;
                    config.TriggerKey = KeyMap[config.TriggerKeyName];
                    SaveConfig();
                }
                modal.Close();
            };

            modal.Controls.AddRange(new Control[] { lblInfo, combo, btnClose });
            modal.ShowDialog(this);
        }

        private void UpdateCommandList()
        {
            var keys = config.Commands.Keys.OrderBy(k => k).ToArray();
            listCommands.Items.Clear();
            listCommands.Items.AddRange(keys);
        }

        private static void LoadConfig()
        {
            if (File.Exists(ConfigFile))
            {
                try
                {
                    config = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFile));

                    if (config == null)
                    {
                        CreateDefaultConfig();
                        return;
                    }

                    if (config.Commands == null)
                    {
                        config.Commands = new Dictionary<string, string>(
                            StringComparer.OrdinalIgnoreCase
                        );
                    }
                }
                catch
                {
                    CreateDefaultConfig();
                }
            }
            else
            {
                CreateDefaultConfig();
            }
        }

        private static void CreateDefaultConfig()
        {
            config = new Config
            {
                Commands = new Dictionary<string, string> { { "ejemplo", "Texto de prueba" } },
            };
            SaveConfig();
        }

        private static void SaveConfig()
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(config, opts));
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (ModifierKeys != Keys.None && (vkCode == 32 || vkCode == 8))
                {
                    buffer = "";
                }
                else if (vkCode == config.TriggerKey)
                {
                    string palabra = buffer.ToLower();
                    if (config.Commands.TryGetValue(palabra, out string textoLargo))
                    {
            Task.Run(() => ExecuteCommand(palabra, textoLargo));
                    }
                    buffer = "";
                }
                else if (vkCode == 8)
                {
                    if (buffer.Length > 0)
                        buffer = buffer[..^1];
                }
                else if (vkCode == 32 || vkCode == 13) // Space o Enter
                {
                    buffer = "";
                }
                else if (
                    (vkCode >= 65 && vkCode <= 90)
                    || (vkCode >= 48 && vkCode <= 57)
                    || (vkCode >= 96 && vkCode <= 105)
                )
                {
                    buffer +=
                        (vkCode >= 96 && vkCode <= 105)
                            ? (vkCode - 96).ToString()
                            : ((char)vkCode).ToString().ToLower();
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static void ExecuteCommand(string palabraEnBuffer, string textoLargo)
        {
            Thread.Sleep(100);
            int totalABorrar = palabraEnBuffer.Length + 1; 
            for (int i = 0; i < totalABorrar; i++)
            {
                keybd_event(VK_BACK, 0, 0, UIntPtr.Zero);
                Thread.Sleep(5);
                keybd_event(VK_BACK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                Thread.Sleep(5);
            }

            Thread staThread = new(() =>
            {
                try
                {
                    Clipboard.SetText(textoLargo);
                }
                catch
                { 
                }
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            Thread.Sleep(50);
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId
        );

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;
        private const byte VK_BACK = 0x08;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    proc,
                    GetModuleHandle(curModule.ModuleName),
                    0
                );
            }
        }
    }
}
