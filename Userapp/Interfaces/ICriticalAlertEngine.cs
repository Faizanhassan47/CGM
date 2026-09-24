namespace CGM.PatientApp.Interfaces;

public interface ICriticalAlertEngine
{
    void TriggerHypoEmergencyAlarm(double glucoseValueMgDl);
    void DismissAlarm();
}
