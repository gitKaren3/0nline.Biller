using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.Wasm.Client.State
{
    /// <summary>
    /// The loaded settings row, shared by the app bar summary and the settings form.
    /// Scoped: one instance per browser session. The form writes it after a save so
    /// the summary updates without a reload.
    /// </summary>
    public sealed class TenantSettingState
    {
        public TenantSetting? Current { get; private set; }

        public event Action? Changed;

        public void Set(TenantSetting settings)
        {
            Current = settings;
            Changed?.Invoke();
        }
    }
}
