# GlucoTrack CGM Patient Mobile App

A modern, clinical-grade Continuous Glucose Monitoring (CGM) patient application built with **.NET MAUI (C# / XAML)** for Android, iOS, Windows, and macOS, designed with aesthetics inspired by Dexcom G7 and Abbott FreeStyle Libre 3.

- **Backend Repository:** [https://github.com/Faizanhassan47/CGM-Backend-](https://github.com/Faizanhassan47/CGM-Backend-)
- **Hardware Simulation:** Compatible with the Windows BLE GATT Peripheral Simulator (`CGM.BleSimulator`).

---

## Quick Start Guide

### 1. Prerequisites
- **.NET 10 SDK** installed.
- **Android SDK & ADB** configured in PATH.
- Physical Android phone with USB Debugging enabled, or Android Emulator.

### 2. Configure Backend URL
Set your API endpoint in `Userapp/.env` (or let it default to `http://127.0.0.1:5232/` for USB reverse):
```ini
CGM_API_BASE_URL=http://127.0.0.1:5232/
CGM_USE_MOCK_SERVICES=false
CGM_USE_REAL_BLE=true
```

### 3. Run on Connected Android Device
```powershell
cd Userapp

# Step A: Forward backend port to phone over USB
adb reverse tcp:5232 tcp:5232

# Step B: Build Android package
dotnet build -f net10.0-android

# Step C: Deploy and launch APK
adb install -r bin\Debug\net10.0-android\com.companyname.cgm.patientapp-Signed.apk
adb shell monkey -p com.companyname.cgm.patientapp -c android.intent.category.LAUNCHER 1
```

### 4. Run on Windows Desktop
```powershell
cd Userapp
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

---

## Features & Clinical UX

1. **Clinical Hero Glucose Badge:**
   - Real-time glucose reading in `mg/dL` or `mmol/L` with directional trend arrows (`↑`, `↗`, `→`, `↘`, `↓`).
   - Dynamic clinical status color-coding: In Target (Green: 70–180), Low/High (Amber), Urgent Low/High (Red).

2. **24-Hour SkiaSharp Trend Graph:**
   - Smooth vector curve with target safe-zone band and interactive interval selector (`3H`, `6H`, `12H`, `24H`).

3. **Quick Event Logging Strip:**
   - Instant action pill buttons: `+ Meal` (carbs), `+ Insulin` (bolus/basal units), and `+ Activity` (workout).
   - Live stream of recent logged events with haptic feedback.

4. **Interactive 7-Day Weekly Calendar Strip:**
   - Responsive horizontal strip showing 7 days with Time In Range (TIR) color dots (🟢 Green $\ge 70\%$, 🟡 Amber $50-69\%$, 🔴 Red $< 50\%$).
   - Tap any day to dynamically inspect that day's readings.

5. **Emergency Hypoglycemia Protocol ("Rule of 15"):**
   - Critical alert protocol card on Alerts tab.
   - Built-in 15-minute countdown timer with interactive controls.
   - One-tap emergency caregiver calling dialer.

6. **BLE Hardware Communication:**
   - Implements diagnostic sequences for hardware verification (`0xE7` serial/firmware, `0xE8` battery, `0xE1` warmup, `0xE2` WE1 current, `0xE3` temperature).
   - Zero hardcoded glucose calculations.

---

## Unit Testing

Run the automated test suite verifying parsers, converters, and device state engines:
```powershell
dotnet test CGM.PatientApp.Tests/CGM.PatientApp.Tests.csproj
```
*(All 49 unit tests passing).*
