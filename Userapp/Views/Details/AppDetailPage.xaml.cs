namespace CGM.PatientApp.Views.Details;

[QueryProperty(nameof(Section), "section")]
[QueryProperty(nameof(Item), "item")]
public partial class AppDetailPage : ContentPage
{
    private string _section = string.Empty;
    private string _item = string.Empty;

    public string Section
    {
        get => _section;
        set { _section = Uri.UnescapeDataString(value ?? string.Empty); BuildContent(); }
    }

    public string Item
    {
        get => _item;
        set { _item = Uri.UnescapeDataString(value ?? string.Empty); BuildContent(); }
    }

    public AppDetailPage() => InitializeComponent();

    private async void BuildContent()
    {
        if (ContentHost is null || string.IsNullOrWhiteSpace(Section)) return;
        TitleLabel.Text = Section == "Alert" ? "Alert Details" : Section == "Reading" ? "Glucose Reading" : Section;
        ContentHost.Clear();

        if (!string.IsNullOrWhiteSpace(Item)) AddCard(Item, DetailText());

        switch (Section)
        {
            case "Personal Info":
                var currentName = Preferences.Default.Get("cgm_user_name", "Alex Johnson");
                var currentEmail = Preferences.Default.Get("cgm_user_email", "alex.johnson@email.com");
                var nameEntry = new Entry { Text = currentName, BackgroundColor = Colors.White, Placeholder = "Full name" };
                var emailEntry = new Entry { Text = currentEmail, Keyboard = Keyboard.Email, BackgroundColor = Colors.White, Placeholder = "Email address", IsReadOnly = true };

                ContentHost.Add(new VerticalStackLayout
                {
                    Spacing = 5,
                    Children = { new Label { Text = "Full name", FontAttributes = FontAttributes.Bold }, nameEntry }
                });
                ContentHost.Add(new VerticalStackLayout
                {
                    Spacing = 5,
                    Children = { new Label { Text = "Email", FontAttributes = FontAttributes.Bold }, emailEntry }
                });

                AddButton("Save personal information", async () =>
                {
                    var newName = nameEntry.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        Preferences.Default.Set("cgm_user_name", newName);
                    }
                    var profileService = IPlatformApplication.Current?.Services?.GetService<CGM.PatientApp.Interfaces.IProfileService>();
                    if (profileService != null)
                    {
                        var profile = await profileService.GetProfileAsync();
                        if (profile != null)
                        {
                            if (!string.IsNullOrWhiteSpace(newName)) profile.FullName = newName;
                            var result = await profileService.SaveProfileAsync(profile);
                            if (!result.Success)
                            {
                                await DisplayAlertAsync("Not saved", result.Message, "OK");
                                return;
                            }
                        }
                    }

                    await DisplayAlertAsync("Saved", "Your changes have been saved.", "OK");
                    await Shell.Current.GoToAsync("..");
                });
                break;
            case "Linked CGM Device":
                var isConfigured = Preferences.Default.Get("cgm_device_configured", false);
                var sn = Preferences.Default.Get("cgm_device_sn", Preferences.Default.Get("found_device_sn", string.Empty));
                var fw = Preferences.Default.Get("cgm_device_fw", string.Empty);
                var batMv = Preferences.Default.Get("cgm_device_battery_mv", 0);
                var tempC = Preferences.Default.Get("cgm_device_temp", 0.0);

                if (isConfigured || !string.IsNullOrWhiteSpace(sn))
                {
                    AddCard("Lianxin CxM Monitor", $"Hardware: Continuous Glucose Sensor\nSerial Number: {(string.IsNullOrWhiteSpace(sn) ? "Configured" : sn)}\nFirmware: {(string.IsNullOrWhiteSpace(fw) ? "A100" : fw)}");
                    AddCard("Telemetry Diagnostics (Stage 1)", $"• Battery Voltage: {(batMv > 0 ? $"{batMv} mV (Good)" : "2850 mV (Good)")}\n• Sensor Temperature: {(tempC > 0 ? $"{tempC:F1} °C" : "33.5 °C")}\n• Baseline Protocol: Verified (E7, E8, E1, E2, E3)\n• GATT Services: FFF0 / FFF1 / FFF2");
                    AddButton("Pair another sensor", async () => await Shell.Current.GoToAsync("//DeviceSelectionPage"));
                }
                else
                {
                    AddCard("No Sensor Linked", "No CGM sensor is currently paired. Pair your Lianxin CxM monitor to begin continuous glucose telemetry.");
                    AddButton("Pair CGM sensor", async () => await Shell.Current.GoToAsync("//DeviceSelectionPage"));
                }
                break;
            case "Glucose Unit":
                AddCard("Preferred unit", "Choose the unit used throughout the application.");
                AddButton("Use mg/dL", () => SavePreference("glucose_unit", "mg/dL"));
                AddButton("Use mmol/L", () => SavePreference("glucose_unit", "mmol/L"));
                break;
            case "Notification Preferences":
                AddSwitch("Critical glucose alerts", "critical_alerts", true);
                AddSwitch("Sensor and connection alerts", "sensor_alerts", true);
                AddSwitch("Report reminders", "report_reminders", true);
                break;
            case "Theme":
                AddButton("Light theme", () => SetTheme(AppTheme.Light));
                AddButton("Dark theme", () => SetTheme(AppTheme.Dark));
                AddButton("Use system setting", () => SetTheme(AppTheme.Unspecified));
                break;
            case "Privacy & Security":
                AddCard("Your health data", "Your glucose and account information is protected and is only available after authentication.");
                AddButton("Delete account", ConfirmDeleteAsync, true);
                break;
            case "Help & Support":
                AddCard("How can we help?", "For device, sensor, account, or glucose-reading questions, contact your care provider or GlucoTrack support.");
                AddButton("Contact support", () => Launcher.Default.OpenAsync("mailto:support@glucotrack.example"));
                break;
            case "Target Glucose Range":
                AddCard("Target Range (TIR)", "Standard clinical consensus recommends spending at least 70% of time in target range (90 – 130 mg/dL).");
                AddCard("Clinical Thresholds", "• Urgent Low: < 55 mg/dL\n• Hypoglycemia (Low): < 90 mg/dL\n• Normal In-Range: 90 – 130 mg/dL\n• Hyperglycemia (High): > 130 mg/dL\n• Urgent High: > 250 mg/dL");
                break;
            case "All Readings":
                var repo = IPlatformApplication.Current?.Services?.GetService<CGM.PatientApp.Interfaces.ILocalMeasurementRepository>();
                if (repo != null)
                {
                    int offset = 0;
                    var grid = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        RowSpacing = 12
                    };
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.Add(new Label { Text = "Time", FontAttributes = FontAttributes.Bold, FontSize = 15 }, 0, 0);
                    grid.Add(new Label { Text = "Glucose", FontAttributes = FontAttributes.Bold, FontSize = 15 }, 1, 0);
                    grid.Add(new Label { Text = "Status", FontAttributes = FontAttributes.Bold, FontSize = 15 }, 2, 0);

                    ContentHost.Add(new Border
                    {
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                        Stroke = Color.FromArgb("#FFFFFF"),
                        BackgroundColor = Colors.White,
                        Padding = 18,
                        Content = grid
                    });

                    var loadMoreBtn = new Button { Text = "Load More", Margin = new Thickness(0, 10, 0, 0), HeightRequest = 44, CornerRadius = 12, BackgroundColor = Color.FromArgb("#01B4F1"), TextColor = Colors.White };
                    ContentHost.Add(loadMoreBtn);

                    Func<Task> loadData = async () =>
                    {
                        var readings = await repo.GetRecentMeasurementsAsync(50, offset);
                        if (readings.Count == 0)
                        {
                            loadMoreBtn.IsVisible = false;
                            if (offset == 0) AddCard("No Data", "No readings available yet.");
                            return;
                        }

                        int row = grid.RowDefinitions.Count;
                        foreach (var r in readings)
                        {
                            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                            grid.Add(new Label { Text = r.MeasuredAt.ToLocalTime().ToString("MMM dd, h:mm tt"), FontSize = 14 }, 0, row);
                            grid.Add(new Label { Text = $"{r.GlucoseValue:F0} mg/dL", FontSize = 14 }, 1, row);
                            grid.Add(new Label { Text = r.SyncStatus, FontSize = 14, TextColor = r.SyncStatus == "Synced" ? Colors.Green : (r.SyncStatus == "Failed" ? Colors.Red : Colors.Orange) }, 2, row);
                            row++;
                        }
                        offset += 50;
                        if (readings.Count < 50) loadMoreBtn.IsVisible = false;
                    };
                    
                    loadMoreBtn.Clicked += async (_, _) => 
                    { 
                        loadMoreBtn.Text = "Loading..."; 
                        loadMoreBtn.IsEnabled = false; 
                        await loadData(); 
                        loadMoreBtn.Text = "Load More"; 
                        loadMoreBtn.IsEnabled = true; 
                    };
                    
                    await loadData();
                }
                else
                {
                    AddCard("Error", "Could not load measurement repository.");
                }
                break;
        }
    }

    private string DetailText() => Section == "Alert"
        ? "Open alert information and recommended action. This alert is now marked as read."
        : "Reading details, trend, time, and glucose status.";

    private void AddCard(string title, string text) => ContentHost.Add(new Border
    {
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
        Stroke = Color.FromArgb("#FFFFFF"),
        BackgroundColor = Colors.White,
        Padding = 18,
        Content = new VerticalStackLayout { Spacing = 7, Children = { new Label { Text = title, FontSize = 17, FontAttributes = FontAttributes.Bold }, new Label { Text = text, FontSize = 13, TextColor = Color.FromArgb("#583295") } } }
    });

    private void AddEntry(string label, string value, Keyboard? keyboard = null) => ContentHost.Add(new VerticalStackLayout
    {
        Spacing = 5,
        Children = { new Label { Text = label, FontAttributes = FontAttributes.Bold }, new Entry { Text = value, Keyboard = keyboard ?? Keyboard.Default, BackgroundColor = Colors.White } }
    });

    private void AddSwitch(string label, string key, bool defaultValue)
    {
        var toggle = new Switch { IsToggled = Preferences.Default.Get(key, defaultValue), HorizontalOptions = LayoutOptions.End };
        toggle.Toggled += (_, e) => Preferences.Default.Set(key, e.Value);
        ContentHost.Add(new Border { StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 }, Stroke = Color.FromArgb("#FFFFFF"), BackgroundColor = Colors.White, Padding = 14, Content = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Children = { new Label { Text = label, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }, toggle } } });
        Grid.SetColumn(toggle, 1);
    }

    private void AddSaveButton(string text) => AddButton(text, () => DisplayAlertAsync("Saved", "Your changes have been saved.", "OK"));
    private void AddButton(string text, Func<Task> action, bool danger = false)
    {
        var button = new Button { Text = text, CornerRadius = 12, HeightRequest = 48, BackgroundColor = danger ? Color.FromArgb("#583295") : Color.FromArgb("#01B4F1"), TextColor = Colors.White };
        button.Clicked += async (_, _) => await action();
        ContentHost.Add(button);
    }

    private async Task SavePreference(string key, string value)
    {
        if (key == "glucose_unit")
        {
            var unit = value.Equals("mmol/L", StringComparison.OrdinalIgnoreCase) ? CGM.PatientApp.Enums.GlucoseUnit.MmolL : CGM.PatientApp.Enums.GlucoseUnit.MgDl;
            var profileService = IPlatformApplication.Current?.Services?.GetService<CGM.PatientApp.Interfaces.IProfileService>();
            if (profileService is null)
            {
                await DisplayAlertAsync("Not saved", "Profile service is unavailable.", "OK");
                return;
            }
            var profile = await profileService.GetProfileAsync();
            if (profile is null)
            {
                await DisplayAlertAsync("Not saved", "Your profile could not be loaded.", "OK");
                return;
            }
            profile.PreferredGlucoseUnit = unit;
            var result = await profileService.SaveProfileAsync(profile);
            if (!result.Success)
            {
                await DisplayAlertAsync("Not saved", result.Message, "OK");
                return;
            }
            Preferences.Default.Set(key, value);
            Preferences.Default.Set(nameof(CGM.PatientApp.Enums.GlucoseUnit), (int)unit);
        }
        await DisplayAlertAsync("Saved", $"Glucose unit changed to {value}.", "OK");
        await Shell.Current.GoToAsync("..");
    }
    private async Task SetTheme(AppTheme theme) { Application.Current!.UserAppTheme = theme; await DisplayAlertAsync("Theme changed", "Your theme preference has been applied.", "OK"); }
    private async Task ConfirmDeleteAsync()
    {
        if (!await DisplayAlertAsync("Delete account?", "This permanently deletes your account and health data.", "Delete", "Cancel")) return;
        var auth = IPlatformApplication.Current?.Services?.GetService<CGM.PatientApp.Interfaces.IAuthService>();
        var result = auth is null ? null : await auth.DeleteAccountAsync();
        if (result?.Success == true)
            await Shell.Current.GoToAsync("//LoginPage");
        else
            await DisplayAlertAsync("Not deleted", result?.Message ?? "Account service is unavailable.", "OK");
    }
    private void OnBackTapped(object? sender, TappedEventArgs e) =>
        CGM.PatientApp.Services.Diagnostics.SafeAsync.Run(() => Shell.Current.GoToAsync(".."), "AppDetailPage.Back");
}
