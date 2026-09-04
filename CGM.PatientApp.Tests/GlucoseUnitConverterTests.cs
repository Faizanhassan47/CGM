using CGM.PatientApp.Enums;
using CGM.PatientApp.Services.Cgm;

namespace CGM.PatientApp.Tests;

public sealed class GlucoseUnitConverterTests
{
    [Theory]
    [InlineData(70, 3.9)]
    [InlineData(180, 10.0)]
    [InlineData(112, 6.2)]
    public void MgDlToMmolL_UsesClinicalConversion(double input, double expected) =>
        Assert.Equal(expected, GlucoseUnitConverter.Convert(input, GlucoseUnit.MgDl, GlucoseUnit.MmolL));

    [Theory]
    [InlineData(3.9, 70)]
    [InlineData(10.0, 180)]
    [InlineData(6.2, 112)]
    public void MmolLToMgDl_UsesClinicalConversion(double input, double expected) =>
        Assert.Equal(expected, GlucoseUnitConverter.Convert(input, GlucoseUnit.MmolL, GlucoseUnit.MgDl));

    [Fact]
    public void NegativeReading_IsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => GlucoseUnitConverter.Convert(-1, GlucoseUnit.MgDl, GlucoseUnit.MmolL));
}
