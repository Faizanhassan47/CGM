using CGM.PatientApp.ViewModels;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views;

public partial class FamilyPage : ContentPage
{
    private readonly FamilyViewModel vm;
    public FamilyPage(FamilyViewModel viewModel) { InitializeComponent(); BindingContext = vm = viewModel; }
    protected override void OnAppearing() { base.OnAppearing(); SafeAsync.Run(vm.LoadAsync, "FamilyPage.OnAppearing"); }
    private void BackClicked(object? sender, EventArgs e) => SafeAsync.Run(() => Shell.Current.GoToAsync(".."), "FamilyPage.Back");
}
