using CotadorLogistico.Core.Settings;

namespace CotadorLogistico.Tests.TestDoubles;

/// <summary>
/// <see cref="ISettingsService"/> falso, guardado só em memória — os testes
/// que dependem de configurações usam este dublê em vez de tocar em disco.
/// </summary>
internal sealed class FakeSettingsService : ISettingsService
{
    private AppSettingsData _data;

    public FakeSettingsService(AppSettingsData? initial = null)
    {
        _data = initial ?? new AppSettingsData();
    }

    public AppSettingsData Get() => _data;

    public void Update(SettingsUpdate update)
    {
        if (!string.IsNullOrWhiteSpace(update.FrenetToken)) _data.FrenetToken = update.FrenetToken;
        if (!string.IsNullOrWhiteSpace(update.MelhorEnvioToken)) _data.MelhorEnvioToken = update.MelhorEnvioToken;
        if (!string.IsNullOrWhiteSpace(update.Theme)) _data.Theme = update.Theme;
    }
}
