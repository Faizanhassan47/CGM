using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels;

public partial class FamilyViewModel(IFamilyService service) : ObservableObject
{
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool hasFamily;
    [ObservableProperty] private bool isOwner;
    [ObservableProperty] private string familyName = string.Empty;
    [ObservableProperty] private string referralCode = string.Empty;
    [ObservableProperty] private string joinCode = string.Empty;
    [ObservableProperty] private string newFamilyName = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    [ObservableProperty] private string lowThreshold = "70";
    [ObservableProperty] private string highThreshold = "180";
    public bool HasNoFamily => !HasFamily;
    public ObservableCollection<FamilyMemberItem> Members { get; } = new();
    partial void OnHasFamilyChanged(bool value) => OnPropertyChanged(nameof(HasNoFamily));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    [RelayCommand]
    public async Task LoadAsync() => await RunAsync(async () => Apply(await service.GetAsync()));

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFamilyName)) { ErrorMessage = "Enter a family name."; return; }
        await RunAsync(async () => Apply(await service.CreateAsync(NewFamilyName.Trim())));
    }

    [RelayCommand]
    private async Task JoinAsync()
    {
        if (string.IsNullOrWhiteSpace(JoinCode)) { ErrorMessage = "Enter a referral code."; return; }
        await RunAsync(async () => Apply(await service.JoinAsync(JoinCode.Trim().ToUpperInvariant())));
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        await Clipboard.Default.SetTextAsync(ReferralCode);
        await Shell.Current.DisplayAlert("Family", "Referral code copied.", "OK");
    }

    [RelayCommand]
    private async Task ShareAsync() => await Share.Default.RequestAsync(new ShareTextRequest
    {
        Text = $"Join my CGM family using referral code: {ReferralCode}", Title = "CGM family referral"
    });

    [RelayCommand]
    private async Task SaveThresholdsAsync()
    {
        if (!double.TryParse(LowThreshold, out var low) || !double.TryParse(HighThreshold, out var high))
        { ErrorMessage = "Enter valid minimum and maximum glucose values."; return; }
        await RunAsync(async () =>
        {
            Apply(await service.UpdateThresholdsAsync(low, high));
            await Shell.Current.DisplayAlertAsync("Alert range saved", $"Family alerts will be sent below {low:0} or above {high:0} mg/dL.", "OK");
        });
    }

    [RelayCommand]
    private async Task ToggleAlertsAsync(FamilyMemberItem member) => await RunAsync(async () =>
    {
        await service.SetAlertsAsync(member.UserId, !member.ReceiveAlerts);
        await LoadCoreAsync();
    });

    [RelayCommand]
    private async Task RemoveAsync(FamilyMemberItem member) => await RunAsync(async () =>
    {
        if (await Shell.Current.DisplayAlert("Remove member", $"Remove {member.FullName}?", "Remove", "Cancel"))
        { await service.RemoveAsync(member.UserId); await LoadCoreAsync(); }
    });

    [RelayCommand]
    private async Task LeaveAsync() => await RunAsync(async () =>
    {
        if (await Shell.Current.DisplayAlert("Leave family", "Leave this family?", "Leave", "Cancel"))
        { await service.LeaveAsync(); Apply(null); }
    });

    private async Task LoadCoreAsync() => Apply(await service.GetAsync());
    private void Apply(CGM.PatientApp.Models.Family? family)
    {
        HasFamily = family != null; Members.Clear();
        if (family == null) { FamilyName = ReferralCode = string.Empty; IsOwner = false; return; }
        FamilyName = family.FamilyName; ReferralCode = family.MyReferralCode; IsOwner = family.IsOwner;
        LowThreshold = family.LowGlucoseThreshold.ToString("0"); HighThreshold = family.HighGlucoseThreshold.ToString("0");
        foreach (var m in family.Members) Members.Add(new FamilyMemberItem(m, family.IsOwner, family.CurrentUserId));
    }
    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return; IsBusy = true; ErrorMessage = string.Empty;
        try { await action(); } catch (Exception ex) { ErrorMessage = ex.Message; } finally { IsBusy = false; }
    }
}

public sealed class FamilyMemberItem(FamilyMember member, bool currentUserIsOwner, int currentUserId)
{
    public int UserId => member.UserId;
    public string FullName => member.FullName;
    public string Email => member.Email;
    public string Role => member.Role;
    public bool ReceiveAlerts => member.ReceiveAlerts;
    public string AlertsText => ReceiveAlerts ? "Alerts: ON" : "Alerts: OFF";
    public bool CanRemove => currentUserIsOwner && !string.Equals(Role, "Owner", StringComparison.OrdinalIgnoreCase);
    public bool CanToggleAlerts => currentUserIsOwner || UserId == currentUserId;
}
