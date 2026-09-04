using CGM.PatientApp.ViewModels.Auth;

namespace CGM.PatientApp.Views.Auth;

public partial class CompleteProfilePage : ContentPage
{
    private readonly CompleteProfileViewModel _viewModel;

    public CompleteProfilePage() : this(IPlatformApplication.Current?.Services?.GetService<CompleteProfileViewModel>() ?? throw new InvalidOperationException("CompleteProfileViewModel not found"))
    {
    }

    public CompleteProfilePage(CompleteProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
