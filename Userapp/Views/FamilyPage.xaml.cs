using CGM.PatientApp.ViewModels;

namespace CGM.PatientApp.Views;

public partial class FamilyPage : ContentPage
{
    private readonly FamilyViewModel vm;
    public FamilyPage(FamilyViewModel viewModel) { InitializeComponent(); BindingContext = vm = viewModel; }
    protected override async void OnAppearing() { base.OnAppearing(); await vm.LoadAsync(); }
    private async void BackClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
